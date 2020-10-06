using System;
using System.Collections;
using System.Linq;

namespace CAengine
{
    public class RuleSet
    {
        private String _survive;
        private String _birth;
        private ArrayList s, b;

        public ArrayList S
        {
            get => s;
        }

        public ArrayList B
        {
            get => b;
        }

        public RuleSet(string survive, string birth)
        {
            _survive = survive;
            _birth = birth;
            s = new ArrayList(survive.Where(Char.IsDigit).ToArray());
            b = new ArrayList(birth.Where(Char.IsDigit).ToArray());
          
        }

        public int getS(int i)
        {
            return (int) ((Char)s[i] - '0');
        }
        public int getB(int i)
        {
            return (int) ((Char)b[i]- '0');
        }
    }
}