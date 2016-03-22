﻿using System.ComponentModel.DataAnnotations;

namespace SalesforceIntegration.Models
{
    public class WebhookModel
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string SObject { get; set; }

        [Required]
        public string Url { get; set; }

        public string Location { get; set; }
    }
}