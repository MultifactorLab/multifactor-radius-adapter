namespace MultiFactor.Radius.Adapter.Tests.E2E;

public class E2ETestSensitiveSettings
{
    public CatalogUser User { get; set; }
    public CatalogUser TechUser { get; set; }
    public CatalogSettings CatalogSettings { get; set; }
    public MultifactorApiSettings MultifactorApiSettings { get; set; }
}

public class CatalogUser
{
    public string UserName { get; set; }
    
    public string Password { get; set; }
}

public class MultifactorApiSettings
{
    public string NasIdentifier { get; set; }
    public string SharedSecret { get; set; }
}

public class CatalogSettings
{
    public string Hosts { get; set; }
    
    public string Groups { get; set; }
}