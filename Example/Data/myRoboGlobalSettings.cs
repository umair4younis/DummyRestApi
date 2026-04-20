namespace Puma.MDE.Data
{
    public class myRoboGlobalSettings
    {
        public int      Id              { get; set; }
        public string   SettingName     { get; set; }
        public string   SettingValue    { get; set; }
        public bool     IsDirty         { get; set; }

        public override string ToString()
        {
            return SettingName + "\t\t\t" + SettingValue;
        }

        public myRoboGlobalSettings Clone()
        {
            myRoboGlobalSettings retval = new myRoboGlobalSettings();

            retval.Id           = Id;
            retval.SettingName  = SettingName;
            retval.SettingValue = SettingValue;

            return retval;
        }

    }
}