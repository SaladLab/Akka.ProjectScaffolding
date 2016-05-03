using System;

namespace Domain
{
    public class HelloGenerator
    {
        private Func<string, string> _generator;

        public HelloGenerator(Func<string, string> generator)
        {
            _generator = generator;
        }

        public string GenerateHello(string who)
        {
            return _generator(who);
        }
    }
}
