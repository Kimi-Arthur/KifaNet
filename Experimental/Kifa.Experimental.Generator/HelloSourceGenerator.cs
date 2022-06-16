using Microsoft.CodeAnalysis;

namespace Kifa.Experimental.Generator
{
    [Generator]
    public class HelloSourceGenerator : ISourceGenerator
    {
        public void Execute(GeneratorExecutionContext context)
        {
            // Code generation goes here
        }
        public void Initialize(GeneratorInitializationContext
            context)
        {
            // No initialization required for this one
        }
    }
}
