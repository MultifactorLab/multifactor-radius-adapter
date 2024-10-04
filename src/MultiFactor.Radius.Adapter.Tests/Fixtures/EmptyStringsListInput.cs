using System.Collections;

namespace MultiFactor.Radius.Adapter.Tests.Fixtures;

internal class EmptyStringsListInput: IEnumerable<object[]>
{
    public IEnumerator<object[]> GetEnumerator()
    {
        yield return new object[] { string.Empty };
        yield return new object[] { " " };
        yield return new object[] { null };
        yield return new object[] { Environment.NewLine };
    }
    
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
