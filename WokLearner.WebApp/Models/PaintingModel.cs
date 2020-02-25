using System.ComponentModel.DataAnnotations;

namespace WokLearner.WebApp.Models
{
    public class PaintingModel
    {
        [Required] public string Author { get; set; }

        [Required] public string Style { get; set; }

        [Required] public string FileName { get; set; }
    }
}