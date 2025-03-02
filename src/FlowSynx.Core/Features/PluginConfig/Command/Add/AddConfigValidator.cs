//using FluentValidation;
//using FlowSynx.Core.Parers.Specifications;
//using FlowSynx.Connectors.Manager;

//namespace FlowSynx.Core.Features.Config.Command.Add;

//public class AddConfigValidator : AbstractValidator<AddConfigRequest>
//{
//    private readonly IConnectorsManager _connectorsManager;
//    private readonly ISpecificationsParser _specificationsParser;

//    public AddConfigValidator(IConnectorsManager connectorsManager, ISpecificationsParser specificationsParser)
//    {
//        _connectorsManager = connectorsManager;
//        _specificationsParser = specificationsParser;
//        RuleFor(request => request.Name)
//            .NotNull()
//            .NotEmpty()
//            .WithMessage(Resources.AddConfigValidatorNameValueMustNotNullOrEmptyMessage);

//        RuleFor(request => request.Name)
//            .Matches("^[a-zA-Z][a-zA-Z0-9_]*$")
//            .WithMessage(Resources.AddConfigValidatorNameValueOnlyAcceptLatingCharacters);

//        RuleFor(request => request.Type)
//            .NotNull()
//            .NotEmpty()
//            .WithMessage(Resources.AddConfigValidatorTypeValueMustNotNullOrEmptyMessage);

//        RuleFor(request => request.Type)
//            .Must(IsTypeValid)
//            .WithMessage(Resources.AddConfigValidatorTypeValueIsNotValid);

//        RuleFor(request => request.Specifications)
//            .Custom(IsSpecificationsValid);
//    }

//    private bool IsTypeValid(string type)
//    {
//        return _connectorsManager.IsExist(type);
//    }

//    private void IsSpecificationsValid(Dictionary<string, string?>? specifications, ValidationContext<AddConfigRequest> context)
//    {
//        var result = _specificationsParser.Parse(context.InstanceToValidate.Type, specifications);
//        if (!result.Valid)
//        {
//            context.AddFailure(result.Message);
//        }
//    }
//}