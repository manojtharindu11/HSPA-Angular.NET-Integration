﻿using System.ComponentModel.DataAnnotations;

namespace web_api.Models
{
    public class User : BaseEntity
    {
        [Required]
        public string UserName { get; set; }

        public string Email { get; set; }

        public string Mobile { get; set; }
        [Required]
        public byte[] Password { get; set; }

        public byte[] PasswordKey { get; set; }
    }
}
