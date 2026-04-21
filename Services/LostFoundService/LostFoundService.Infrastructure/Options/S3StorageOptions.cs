namespace LostFoundService.Infrastructure.Options
{
    public class S3StorageOptions
    {
        public const string SectionName = "S3";

        public string BucketName { get; set; } = string.Empty;
        public string Region { get; set; } = "eu-central-1";
        public string? AccessKey { get; set; }
        public string? SecretKey { get; set; }
        public string? ServiceUrl { get; set; }
        public bool ForcePathStyle { get; set; }
        public string? PublicBaseUrl { get; set; }
        public string KeyPrefix { get; set; } = "lost-and-found";
        public bool AutoCreateBucket { get; set; }
    }
}
