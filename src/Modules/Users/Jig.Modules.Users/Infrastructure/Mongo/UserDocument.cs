using Jig.Modules.Users.Domain;
using Jig.SharedKernel;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Jig.Modules.Users.Infrastructure.Mongo;

/// <summary>The Mongo storage shape, which is deliberately not the domain type.
///
/// The pseudo key does the work a counters collection would otherwise have to: the domain has
/// already minted the id by the time this document exists, so _id is just that value written
/// down. No ObjectId, no sequence, and nothing here that has to be undone if the store changes
/// again.</summary>
internal sealed class UserDocument
{
    // The driver refuses to serialize a bare Guid unless told which BSON representation to use
    // (legacy drivers guessed, and guessed differently across languages); Standard is the
    // portable, spec-compliant one.
    [BsonId]
    [BsonGuidRepresentation(GuidRepresentation.Standard)]
    public Guid Id { get; set; }

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("email")]
    public string Email { get; set; } = string.Empty;

    public User ToDomain() => new(new PseudoKey(Id), Name, Email);

    public static UserDocument From(User user) => new()
    {
        Id = user.Id.Value,
        Name = user.Name,
        Email = user.Email,
    };
}
