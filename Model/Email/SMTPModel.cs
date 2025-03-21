namespace api_preven_email_service.Model.Email{
    public class SMTPModel{
        public SMTPModel(){
            host = string.Empty;
            port = 0;
            user = string.Empty;
            pass = string.Empty;
        }

        public string host { get; set; }
        public int port { get; set; }
        public string user { get; set; }
        public string pass { get; set; } 
    }
}