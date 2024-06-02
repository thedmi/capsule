using Capsule.Attribution;

namespace Capsule.Test.AutomatedTests.UnitTests;

// This file contains various constructs that affect code generation and are here to verify that the generated code
// does actually compile.

public class CodeGenTest
{
    public class Inner
    {
        // Verify that deeply nested types can be used as Capsule
        [Capsule]
        public class CodeGenTest
        {
            [Expose]
            public async Task DoIt() { }
        }
    }
}
