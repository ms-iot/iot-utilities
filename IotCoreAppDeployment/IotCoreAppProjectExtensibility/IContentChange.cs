namespace Microsoft.Iot.IotCoreAppProjectExtensibility
{
    public interface IContentChange
    {
        bool ApplyToContent(string rootFolder);
    }
}