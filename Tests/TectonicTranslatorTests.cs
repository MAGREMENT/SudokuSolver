using Model.Tectonic;

namespace Tests;

public class TectonicTranslatorTests
{
    [Test]
    public void TranslateTest()
    {
        const string s = "5.4:2.0;.0;4.0;.0;.1;.1;.1;.1;.1;.2;.2;.2;.3;.4;5.4;.4;.3;3.3;.4;3.4;";

        var t = TectonicTranslator.TranslateLineFormat(s);

        Console.WriteLine(t);
    }
}