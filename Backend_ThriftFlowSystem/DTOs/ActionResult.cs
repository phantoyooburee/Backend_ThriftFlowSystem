namespace Backend_ThriftFlowSystem.DTOs
{
    public class ActionResult
    {
        public string Code { get; set; } = "";
        public string Value { get; set; } = "";
        public string Description { get; set; } = "";
        public string ShowText { get; set; } = "";

        public ActionResult() { }

        public void ToSuccessStatus(string code)
        {
            this.Code = code;
            this.Value = "P";
            this.ShowText = "Success";
            this.Description = "Success";
        }

        public void ToSuccessStatus(string msgToShow, string code)
        {
            this.Code = code;
            this.Value = "P";
            this.ShowText = msgToShow;
            this.Description = msgToShow;
        }

        public void ToSuccessStatus()
        {
            this.Code = "P";
            this.Value = "P";
            this.ShowText = "Success";
            this.Description = "Success";
        }

        public void ToErrorStatus(string showText, string code)
        {
            this.Code = code;
            this.Value = "F";
            this.ShowText = showText;
            this.Description = showText;
        }

        public void ToErrorStatus()
        {
            this.Code = "F";
            this.Value = "F";
            this.ShowText = "Failure";
            this.Description = "Failure";
        }
    }
}
