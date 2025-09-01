using Microsoft.Extensions.Logging;

namespace PoFastType.Api.Services
{
    /// <summary>
    /// A text generation strategy that returns predefined hardcoded text for testing purposes.
    /// This strategy follows the Strategy pattern to provide a deterministic alternative to AI generation.
    /// </summary>
    public class HardcodedTextStrategy : ITextGenerationStrategy
    {
        private readonly ILogger<HardcodedTextStrategy> _logger;

        public string StrategyName => "Hardcoded";

        public HardcodedTextStrategy(ILogger<HardcodedTextStrategy> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<string> GenerateTextAsync()
        {
            _logger.LogInformation("Generating hardcoded text for typing practice");

            var texts = new List<string>
            {
                "The quick brown fox jumps over the lazy dog. This pangram contains every letter of the alphabet at least once. It has been used for typing practice for decades because it covers all the keys on a keyboard. When practicing typing, it's important to focus on accuracy first, then speed. Many professional typists can type this sentence at speeds exceeding 100 words per minute while maintaining perfect accuracy. The sentence demonstrates proper finger placement across all keyboard zones including the home row, upper row, and lower row. Regular practice with varied text improves muscle memory and typing fluency. Whether you're a beginner learning to type or an expert honing your skills, consistent practice with diverse content will help you achieve your typing goals. Remember that typing is a skill that improves with deliberate practice and patience.",
                "Never underestimate the power of a good book. Reading expands your mind, broadens your horizons, and introduces you to new ideas and perspectives. It's a journey of discovery that can take you to far-off lands or deep into the human psyche. Make reading a regular habit, and you'll find yourself growing intellectually and emotionally with every page. The quiet act of turning pages can be a profound experience, offering both escape and enlightenment. Dive into different genres, explore various authors, and let the stories transport you. Reading is not just a pastime; it's an essential tool for lifelong learning and personal development.",
                "The early bird catches the worm, but the second mouse gets the cheese. This old adage highlights the importance of timing and observation. Sometimes, being first isn't always the best strategy; waiting and learning from others' mistakes can lead to greater success. In life, it's crucial to balance ambition with prudence. Don't rush into things without proper consideration, but also don't hesitate when the right opportunity arises. Wisdom often comes from understanding when to act and when to observe. Be adaptable, learn from every situation, and always be ready to seize the moment when it truly counts.",
                "Innovation is the key to progress. In a rapidly changing world, those who embrace new ideas and technologies are the ones who thrive. Continuous improvement and a willingness to experiment are essential for staying ahead. Don't be afraid to challenge the status quo and think outside the box. Every great invention started as a bold idea, and every significant advancement was once a daring experiment. Foster a culture of creativity and curiosity, and you'll unlock endless possibilities. The future belongs to those who are not afraid to innovate and push the boundaries of what's possible.",
                "The sun always shines brightest after the rain. This metaphor reminds us that even after difficult times, brighter days are ahead. Challenges are an inevitable part of life, but they also offer opportunities for growth and resilience. Embrace adversity, learn from your struggles, and emerge stronger on the other side. Just as a storm cleanses the air and nourishes the earth, hardships can purify your spirit and deepen your character. Keep your spirits high, maintain a positive outlook, and trust that every cloud has a silver lining. Hope is a powerful force that can guide you through the darkest moments."
            };

            var random = new Random();
            var index = random.Next(texts.Count);
            var text = texts[index];

            await Task.Delay(10); // Simulate async operation

            _logger.LogInformation("Successfully generated hardcoded text with {Length} characters", text.Length);
            return text.Trim();
        }
    }
}
