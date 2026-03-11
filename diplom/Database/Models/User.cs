using DataModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Database.Models
{
    public class User
    {
        public int Id { get; private set; }
        [Required]
        public string Username { get; private set; } = string.Empty;
        [Required]
        public string PasswordHash { get; private set; }
        [Required]
        [EmailAddress]
        public string Email { get; private set; }
        public DateTime CreatedAt { get; private set; }

        public static User? Create(UserModel user)
        {
            if (user == null)
            {
                return null;
            }
            return new User
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                PasswordHash = user.PasswordHash,
                CreatedAt = user.CreatedAt,
            };
        }

        public void Update(UserModel user)
        {
            if (user == null)
            {
                return;
            }

            Username = user.Username;
            Email = user.Email;
            PasswordHash = user.PasswordHash;
            CreatedAt = user.CreatedAt;
        }

        public UserModel GetViewModel => new()
        {
            Id = Id,
            Email = Email,
            Username = Username,
            PasswordHash = PasswordHash,
            CreatedAt = CreatedAt
        };
    }
}
