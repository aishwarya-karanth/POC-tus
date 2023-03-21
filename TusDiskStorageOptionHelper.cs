namespace POC_tus
{
    public class TusDiskStorageOptionHelper
    {
        public string StorageDiskPath { get; }

        public TusDiskStorageOptionHelper()
        {
            string path = Path.Combine(Environment.CurrentDirectory, "App_Data", "tusfiles");
            if (!File.Exists(path))
                Directory.CreateDirectory(path);

            StorageDiskPath = path;
        }
    }
}
