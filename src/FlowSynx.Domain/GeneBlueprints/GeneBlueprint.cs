using FlowSynx.Domain.Exceptions;
using FlowSynx.Domain.GeneBlueprints.Events;
using FlowSynx.Domain.Primitives;
using FlowSynx.Domain.Tenants;
using FlowSynx.Domain.ValueObjects;

namespace FlowSynx.Domain.GeneBlueprints;

public class GeneBlueprint : AuditableEntity<GeneBlueprintId>, IAggregateRoot, ITenantScoped, IUserScoped
{
    public TenantId TenantId { get; set; }
    public string UserId { get; set; }
    public string GeneticBlueprint { get; private set; }
    public string Generation { get; private set; }
    public string Phenotypic { get; private set; }
    public string Annotation { get; private set; }
    public List<NucleotideSequence> NucleotideSequences { get; private set; }   // Parameters
    public ExpressionProfile ExpressionProfile { get; private set; }
    public EpistaticInteraction EpistaticInteraction { get; private set; }      // Compatibility Matrix
    public DefenseMechanism DefenseMechanism { get; private set; }              // Immune Response
    public ExpressedProtein ExpressedProtein { get; private set; }              // ExecutableComponent
    public Dictionary<string, object> EpigeneticMarks { get; private set; }     // Metadata
    public Tenant? Tenant { get; private set; }

    // Domain invariants
    private GeneBlueprint() { } // For EF Core

    public GeneBlueprint(
        TenantId tenantId,
        string userId,
        GeneBlueprintId id,
        string geneticBlueprint,
        string generation,
        string phenotypic,
        string annotation,
        List<NucleotideSequence> nucleotideSequences,
        ExpressionProfile expressionProfile,
        EpistaticInteraction epistaticInteraction,
        DefenseMechanism defenseMechanism,
        ExpressedProtein expressedProtein)
    {
        TenantId = tenantId ?? throw new ArgumentNullException(nameof(tenantId));
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        Id = id ?? throw new ArgumentNullException(nameof(id));
        GeneticBlueprint = geneticBlueprint ?? throw new ArgumentNullException(nameof(geneticBlueprint));
        Generation = generation ?? throw new ArgumentNullException(nameof(generation));
        Phenotypic = phenotypic ?? throw new ArgumentNullException(nameof(phenotypic));
        Annotation = annotation ?? throw new ArgumentNullException(nameof(annotation));
        NucleotideSequences = nucleotideSequences ?? new List<NucleotideSequence>();
        ExpressionProfile = expressionProfile ?? throw new ArgumentNullException(nameof(expressionProfile));
        EpistaticInteraction = epistaticInteraction ?? throw new ArgumentNullException(nameof(epistaticInteraction));
        DefenseMechanism = defenseMechanism ?? throw new ArgumentNullException(nameof(defenseMechanism));
        ExpressedProtein = expressedProtein ?? throw new ArgumentNullException(nameof(expressedProtein));
        EpigeneticMarks = new Dictionary<string, object>();

        ValidateState();
        AddDomainEvent(new GeneBlueprintCreated(this));
    }

    public void Update(
        string? phenotypic = null,
        string? annotation = null,
        ExpressionProfile? expressionProfile = null,
        DefenseMechanism? defenseMechanism = null,
        Dictionary<string, object>? epigeneticMarks = null)
    {
        if (phenotypic != null) Phenotypic = phenotypic;
        if (annotation != null) Annotation = annotation;
        if (expressionProfile != null) ExpressionProfile = expressionProfile;
        if (defenseMechanism != null) DefenseMechanism = defenseMechanism;
        if (epigeneticMarks != null) EpigeneticMarks = epigeneticMarks;

        ValidateState();
        AddDomainEvent(new GeneBlueprintUpdated(this));
    }

    private void ValidateState()
    {
        if (string.IsNullOrWhiteSpace(Generation))
            throw new DomainException("Gene blueprint generation cannot be empty");

        if (string.IsNullOrWhiteSpace(Phenotypic))
            throw new DomainException("Gene blueprint phenotypic cannot be empty");

        if (string.IsNullOrWhiteSpace(Annotation))
            throw new DomainException("Gene blueprint annotation cannot be empty");

        if (ExpressedProtein == null)
            throw new DomainException("Expressed protein is required");
    }

    public bool HasDefenseMechanism() => DefenseMechanism != null;

    public string GetGeneticBlueprintId() =>
        string.IsNullOrEmpty(GeneticBlueprint) ? Id.Value : GeneticBlueprint;
}