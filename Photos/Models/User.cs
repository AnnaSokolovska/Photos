using Neo4jClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web;

namespace Photos.Models
{
    public class User
    {
        public int idUser { get; set; }
        public string Name { get; set; }  
        public string Surname { get; set; }
        public string NickName { get; set; }
        public string Avatar { get; set; }
        public int Follovers { get; set; }
        public int Follovings { get; set; }
        public int Posts { get; set; }
    }

    public class Post
    {
        public int idUserPosted { get; set; }
        public string Photo { get; set; }
        public DateTimeOffset Data { get; set; }
        public string Text { get; set; }
        public int Likes { get; set; }
        public int Comments { get; set; }
        //public bool Equals(Post other)
        //{
        //    if (Object.ReferenceEquals(other, null)) return false;
        //    if (Object.ReferenceEquals(this, other)) return true;
        //    return this.idUserPosted.Equals(other.idUserPosted) && this.Photo.Equals(other.Photo);
        //}
        //public override int GetHashCode()
        //{
        //    int hashProductName = idUserPosted == null ? 0 : idUserPosted.GetHashCode();
        //    int hashProductCode = this.Photo.GetHashCode();
        //    return hashProductName ^ hashProductCode;
        //}
    }

    public class Comment
    {
        public int idUserLeft { get; set; }
        public string Photo { get; set; }
        public string UserData { get; set; }
        public string Text { get; set; }
        public DateTimeOffset Data { get; set; }
        public string Avatar { get; set; }
        public long CreationTime {get; set;}
    }

    public class UserFeed
    {
        public int idUser { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string NickName { get; set; }
        public string Avatar { get; set; }
        public string Photo { get; set; }
        public DateTimeOffset Data { get; set; }
        public string Text { get; set; }
        public int Likes { get; set; }
        public int Comments { get; set; }
    }
}