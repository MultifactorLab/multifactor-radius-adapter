namespace MultiFactor.Radius.Adapter.Configuration.Features.UserNameTransformFeature
{
    public class UserNameTransformRule
    {
        public UserNameTransformRulesElement Element { get; init; }
        public UserNameTransformRulesScope Scope { get; init; }

        public UserNameTransformRule(UserNameTransformRulesElement element, UserNameTransformRulesScope scope)
        {
            Element = element;
            Scope = scope;
        } 
    }
}
