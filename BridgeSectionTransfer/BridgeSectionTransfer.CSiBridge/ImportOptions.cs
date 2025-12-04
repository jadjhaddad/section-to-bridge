namespace BridgeSectionTransfer.CSiBridge
{
    public class ImportOptions
    {
        public bool SetReferencePoint { get; set; } = true;
        public bool ClearExistingVoids { get; set; } = true;
        public string TargetSectionName { get; set; } = "";
        public bool CreateNewSection { get; set; } = true;
    }
}
