# Seeing the runtime for yourself

This is the walk that turns the tests into something you can watch. The suite is the proof of record; this is the live version.

## Bring up the app and the dashboard

```
docker compose up --build
```

That starts two containers: the Jig API on `http://localhost:8080`, and the standalone Aspire Dashboard on `http://localhost:18888`. The app exports its traces and metrics to the dashboard over OTLP.

## Watch one trace cross the channel

Register a user. Since Part 7 the endpoint requires auth, so the compose file ships a demo machine key (`demo-key`) to keep this a single curl; a real deployment would send a bearer token from an identity provider instead.

```
curl -i -X POST http://localhost:8080/v1/users -H "X-Api-Key: demo-key" -H "content-type: application/json" -d "{\"name\":\"Ada\",\"email\":\"ada@example.com\"}"
```

Open the dashboard at `http://localhost:18888` and find the trace for that request. The point is the shape of it: the HTTP span for `POST /v1/users` and a separate `integration-event UserRegistered` span sit under one trace id, even though the second one runs on the background worker, not the request thread. That is the captured trace context doing its job across the hand-off.

## Watch the metrics move

In the dashboard's metrics view, the `Jig` meter carries `jig.integration_events.processed`, `jig.integration_events.duration`, and `jig.integration_events.queue_depth`. Register a few more users and watch the processed count climb and the queue depth rise and fall.

## Watch the drain on shutdown

Stop just the app container and read its logs as it goes:

```
docker compose stop app
docker compose logs app
```

The readiness gate closes first (a `/health/ready` probe would now return 503 while `/health/live` stays 200), then the pump drains whatever was buffered when shutdown began, bounded by the 30 second shutdown timeout.

One honest limit, because this design is deliberately non-durable: hosted services stop before the HTTP server does, so an event published by a request that is itself still finishing during the shutdown window can land in the channel after the pump has already drained and exited, and that one is lost. Events already queued when shutdown starts are the ones drained here. A crash loses in-flight events the same way. Durability, a persistent outbox, is a later problem, and the reason is Part 5: each module owns its store and it might be Mongo, so there is no single transaction to enroll an outbox write into.

Backpressure is by design too. The channel is bounded at 1024. If the worker falls far enough behind to fill it, publishing blocks the calling request until space frees, so a sustained backlog pushes latency back onto the request thread. That is the pressure valve working, not a bug.
