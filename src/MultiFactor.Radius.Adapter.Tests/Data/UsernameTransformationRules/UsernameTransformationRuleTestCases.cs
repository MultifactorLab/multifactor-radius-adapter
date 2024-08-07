namespace MultiFactor.Radius.Adapter.Tests.Data.UsernameTransformationRules
{
    public class UsernameTransformationRuleTestCases
    {
        public static IEnumerable<object[]> TestCase1
        {
            get
            {
                yield return new object[]
                {
                    new UsernameTransformationRuleTestCase
                    {
                        Asset = "username-transformation-rule-before-first-fa.config",
                        ReplaceFirst = "$0@test.local",
                        MatchFirst = "(.+)",
                        ReplaceSecond = "$0@tes1t.local",
                        MatchSecond = "(.+)",
                     },
                };

                yield return new object[]
                {
                    new UsernameTransformationRuleTestCase
                    {
                        Asset = "username-transformation-rule-legacy.config",
                        ReplaceFirst = "$0@test.local",
                        MatchFirst = "(.+)",
                        ReplaceSecond = "$0@test.local",
                        MatchSecond = "(.+)",
                    }
                };
            }
        }
    }
}
