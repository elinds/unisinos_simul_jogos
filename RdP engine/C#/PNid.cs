namespace RdPengine
{
    public class PNid
    {
        public string Name
        {
            get => _name;
            set => _name = value;
        }

        public int Instance
        {
            get => _instance;
            set => _instance = value;
        }

        private string _name;
        private int _instance;
        
        public override string ToString()
        {
            return (_name + "." + Instance.ToString());
        }
    }
}