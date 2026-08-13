namespace Sample
{
    // This class is used to validate that Minicover can instrument assemblies whose PDB records a
    // document for a file that does not exist. A #line directive attributes code to the generator
    // input it names - what T4, Razor and ANTLR emit - and the compiler records that name as a
    // document with no checksum, because it never read a file to hash.
    public static class ClassWithLineDirective
    {
        public static int Compute(int value)
        {
#line 10 "ClassWithLineDirective.template"
            if (value > 0)
            {
                value++;
            }
#line default

            return value;
        }
    }
}
