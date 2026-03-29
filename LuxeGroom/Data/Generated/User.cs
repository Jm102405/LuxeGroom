using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace LuxeGroom.Data.Generated;

// Unique index on Username column
[Index("Username", Name = "UQ__Users__536C85E450D7833E", IsUnique = true)]
// Unique index on Gmail column
[Index("Gmail", Name = "UQ__Users__B488B103261A99EE", IsUnique = true)]
public partial class User
{
    // Primary key — role-prefixed sequential ID (ADM-1, USR-1)
    [Key]
    [Column("UserID")]
    [StringLength(20)]
    [Unicode(false)]
    public string UserId { get; set; } = null!;

    // Unique login username
    [StringLength(100)]
    public string Username { get; set; } = null!;

    // BCrypt hashed password
    [StringLength(255)]
    public string PasswordHash { get; set; } = null!;

    // Unique Gmail address used for OTP and welcome emails
    [StringLength(150)]
    public string Gmail { get; set; } = null!;

    // User role — Admin or Staff
    [StringLength(20)]
    public string Role { get; set; } = null!;

    // Account status — Active or Inactive
    [StringLength(20)]
    public string Status { get; set; } = null!;

    // Timestamp of when the account was created
    [Column(TypeName = "datetime")]
    public DateTime DateCreated { get; set; }

    // Optional phone number for SMS recovery
    [StringLength(20)]
    public string? PhoneNumber { get; set; }

    // 6-digit OTP code for password reset — null when not in use
    [StringLength(6)]
    public string? ResetCode { get; set; }

    // Expiry datetime for the OTP code — null when not in use
    [Column(TypeName = "datetime")]
    public DateTime? ResetCodeExpiry { get; set; }
}
