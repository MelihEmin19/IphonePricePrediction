namespace IphonePriceWeb.Data.Entities
{
    /// <summary>
    /// Kullanıcı Entity
    /// </summary>
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Navigation
        public ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();
    }

    /// <summary>
    /// Marka Entity
    /// </summary>
    public class Brand
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        // Navigation
        public ICollection<Model> Models { get; set; } = new List<Model>();
    }

    /// <summary>
    /// Model Entity
    /// </summary>
    public class Model
    {
        public int Id { get; set; }
        public int BrandId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int ReleaseYear { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation
        public Brand? Brand { get; set; }
        public ICollection<Spec> Specs { get; set; } = new List<Spec>();
    }

    /// <summary>
    /// Spec Entity
    /// </summary>
    public class Spec
    {
        public int Id { get; set; }
        public int ModelId { get; set; }
        public int RamGb { get; set; }
        public int StorageGb { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation
        public Model? Model { get; set; }
        public ICollection<Listing> Listings { get; set; } = new List<Listing>();
        public ICollection<Prediction> Predictions { get; set; } = new List<Prediction>();
    }

    /// <summary>
    /// Listing Entity
    /// </summary>
    public class Listing
    {
        public int Id { get; set; }
        public int SpecId { get; set; }
        public decimal Price { get; set; }
        public string Condition { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string? Url { get; set; }
        public DateTime ScrapedAt { get; set; }
        public bool IsActive { get; set; }

        // Navigation
        public Spec? Spec { get; set; }
    }

    /// <summary>
    /// Prediction Entity
    /// </summary>
    public class Prediction
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public int SpecId { get; set; }
        public string ConditionValue { get; set; } = string.Empty;
        public decimal PredictedPrice { get; set; }
        public decimal? ConfidenceScore { get; set; }
        public DateTime CreatedAt { get; set; }

        // Navigation
        public User? User { get; set; }
        public Spec? Spec { get; set; }
    }
}

