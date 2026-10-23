namespace DerbyArena.Mission
{
    public enum MissionPhase
    {
        Hunt,
        Extract,
        Won,
        Lost
    }

    public sealed class MissionRules
    {
        public const int CellsRequired = 3;
        public float TimeLeft = 240f;
        public float ExtractHoldRequired = 6f;
        public int Cells;
        public float ExtractHold;
        public bool PlayerDead;
        public MissionPhase Phase = MissionPhase.Hunt;

        public void CollectCell()
        {
            if (Phase != MissionPhase.Hunt)
                return;
            if (Cells < CellsRequired)
                Cells++;
            if (Cells >= CellsRequired)
                Phase = MissionPhase.Extract;
        }

        public void Tick(float dt, bool insideExtract)
        {
            if (Phase == MissionPhase.Won || Phase == MissionPhase.Lost)
                return;
            TimeLeft -= dt;
            if (PlayerDead || TimeLeft <= 0f)
            {
                Phase = MissionPhase.Lost;
                return;
            }
            if (Phase != MissionPhase.Extract)
                return;
            if (insideExtract)
            {
                ExtractHold += dt;
                if (ExtractHold >= ExtractHoldRequired)
                    Phase = MissionPhase.Won;
            }
            else if (ExtractHold > 0f)
            {
                ExtractHold -= dt * 0.35f;
                if (ExtractHold < 0f)
                    ExtractHold = 0f;
            }
        }
    }
}
