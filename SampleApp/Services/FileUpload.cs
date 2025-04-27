namespace SampleApp.Services
{
    public class FileUpload
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;

        public FileUpload(IWebHostEnvironment webHostEnvironment, IConfiguration configuration)
        {
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
        }


        public async Task<string> UploadFileAsync(IFormFile file)
        {
            Console.WriteLine("Move to file upload");
            // Check if the file is valid
            if (file == null || file.Length == 0)
            {
                return null;
            }

            // Generate a unique filename with its extension
            var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);


            // Retrieve the upload folder path from web.config
            string uploadFolder = _configuration["appSettings:UploadFolder"];


            //if (string.IsNullOrEmpty(uploadFolder))
            //{
            //    // Provide a default value if not set
            //    uploadFolder = "Uploads";
            //}

            // Map the relative folder to a physical path using the WebRootPath.
            var uploadsFolderPath = Path.Combine(_webHostEnvironment.WebRootPath, uploadFolder);
            if (!Directory.Exists(uploadsFolderPath))
            {
                Directory.CreateDirectory(uploadsFolderPath);
            }

            // Full physical file path
            var filePath = Path.Combine(uploadsFolderPath, fileName);

            // Save the file to disk asynchronously
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Return only the filename (with extension) for database storage
            return fileName;
        }

    }
}
