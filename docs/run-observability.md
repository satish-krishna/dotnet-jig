# Seeing the runtime for yourself

This is the walk that turns the tests into something you can watch. The suite is the proof of record; this is the live version.

## Bring up the app and the dashboard

```
docker compose up --build
```

That starts two containers: the Jig API on `http://localhost:8080`, and the standalone Aspire Dashboard on `http://localhost:18888`. The app exports its traces and metrics to the dashboard over OTLP.

## Watch one trace cross the channel

Register a user:

```
curl -i -X POST http://localhost:8080/v1/users -H "content-type: application/json" -d "{\"name\":\"Ada\",\"email\":\"ada@example.com\"}"
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

The readiness gate closes first (a `/health/ready` probe would now return 503 while `/health/live` stays 200), then the pump drains whatever was still buffered before the process exits, bounded by the 30 second shutdown timeout. Nothing in flight is dropped.
