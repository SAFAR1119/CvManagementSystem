namespace CvManagementSystem.Services;

public interface IDropboxService
{
    Task UploadJsonAsync(string fileName, string json);
}