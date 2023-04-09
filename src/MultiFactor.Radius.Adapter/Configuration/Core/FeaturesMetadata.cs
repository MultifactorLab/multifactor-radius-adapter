namespace MultiFactor.Radius.Adapter.Configuration.Core
{
    public interface IFeatureDescriptor
    {
        string Token { get; }
        string Description { get; }
        FeatureScope Scope { get; }
        bool IsRequired(string value);
    }

    public enum FeatureScope
    {
        Root,
        Client,
        Both
    }

    public class FeaturesMetadata
    {
    }
}
