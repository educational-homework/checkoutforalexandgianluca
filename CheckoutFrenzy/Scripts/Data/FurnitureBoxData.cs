namespace CryingSnow.CheckoutFrenzy
{
    [System.Serializable]
    public class FurnitureBoxData
    {
        public int FurnitureID { get; set; }
        public Location Location { get; set; }
        public Orientation Orientation { get; set; }

        public FurnitureBoxData(FurnitureBox furnitureBox)
        {
            FurnitureID = furnitureBox.furnitureId;
            Location = new Location(furnitureBox.transform.position);
            Orientation = new Orientation(furnitureBox.transform.rotation);
        }
    }
}
