namespace CryingSnow.CheckoutFrenzy
{
    [System.Serializable]
    public class CleanableData
    {
        public int CleanableID { get; private set; }
        public Location Location { get; private set; }
        public Orientation Orientation { get; private set; }

        public CleanableData(Cleanable cleanable)
        {
            CleanableID = cleanable.CleanableID;
            Location = new Location(cleanable.transform.position);
            Orientation = new Orientation(cleanable.transform.rotation);
        }
    }
}
