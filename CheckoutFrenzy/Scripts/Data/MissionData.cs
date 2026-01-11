namespace CryingSnow.CheckoutFrenzy
{
    [System.Serializable]
    public class MissionData
    {
        public int MissionID { get; set; }
        public int Progress { get; set; }
        public bool IsComplete { get; set; }

        public MissionData(int missionId)
        {
            MissionID = missionId;
        }
    }
}
