
namespace FullTopBuilder
{
    public class PlayerModel
    {
        public string Username { get; set; }
        public string LivePP { get; set; }
        public string LocalPP { get; set; }
        public double LocalPPNumeric { get; set; }
        public string PPChange { get; set; }

        public override string ToString() => Username;
    }
}
