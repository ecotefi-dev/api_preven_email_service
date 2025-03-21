namespace api_preven_email_service.Helper
{
    public static class ConvertirIFormFileABytes {
        public static async Task<byte[]> ConvertirABytesAsync(IFormFile archivo)
        {
            using (var memoryStream = new MemoryStream())
            {
                await archivo.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}