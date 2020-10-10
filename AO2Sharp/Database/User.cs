﻿using SQLite;
using System;

namespace AO2Sharp.Database
{
    [Table("users")]
    public class User
    {
        [PrimaryKey, Indexed]
        [Column("hdid")]
        public string Hdid { get; set; }
        [Column("ips")]
        public string Ips { get; set; }
        [Column("banned")]
        public bool Banned { get; set; }
        [Column("banreason")]
        public string BanReason { get; set; }
        [Column("expiration")]
        public DateTime? BanExpiration { get; set; }
    }
}