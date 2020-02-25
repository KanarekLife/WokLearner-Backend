namespace WokLearner.WebApp.Models
{
    public class JwtSettings
    {
        public string Audience { get; set; }
        public string Issuer { get; set; }
        public int ExpireTimeInSeconds { get; set; }
        public string Key { get; set; }
    }
}