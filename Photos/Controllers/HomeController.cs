using Neo4jClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Photos.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Photos.Controllers
{
    public class HomeController : Controller
    {
        public Collection<User> users = new Collection<User>();
        public Collection<Post> posts = new Collection<Post>();
        public Collection<Comment> commentss = new Collection<Comment>();
        public Collection<UserFeed> userfeed = new Collection<UserFeed>();

        public ActionResult Followings()
        {
            var client = new GraphClient(new Uri("http://localhost:7474/db/data"));
            client.Connect();
            int id = Convert.ToInt32(Session["idUser"]);
            var u = client.Cypher
                       .OptionalMatch("(user1:User)-[FOLLOW]->(user2:User)")
                       .Where((User user1) => user1.idUser == id)
                       .ReturnDistinct(user2 => user2.As<User>())
                       .Results;
            List<User> U = u.ToList();
            foreach (User us in U)
            {
                if (us != null)
                {
                    users.Add(us);
                }
            }
            return View(users);
        }
        public ActionResult Followers()
        {
            var client = new GraphClient(new Uri("http://localhost:7474/db/data"));
            client.Connect();
            int id = Convert.ToInt32(Session["idUser"]);
            var u = client.Cypher
                       .OptionalMatch("(user1:User)<-[FOLLOW]-(user2:User)")
                       .Where((User user1) => user1.idUser == id)
                       .ReturnDistinct(user2 => user2.As<User>())
                       .Results;
            List<User> U = u.ToList();
            foreach (User us in U)
            {
                if (us != null)
                {
                    users.Add(us);
                }
            }
            return View(users);
        }
        public ActionResult Feed ()
        {
            var client = new GraphClient(new Uri("http://localhost:7474/db/data"));
            client.Connect();
            int id = Convert.ToInt32(Session["idUser"]);
            var u = client.Cypher
                       .OptionalMatch("(user1:User)-[FOLLOW]->(user2:User)")
                       .Where((User user1) => user1.idUser == id)
                       .ReturnDistinct(user2 => user2.As<User>())
                       .Results;
            List<User> U = u.ToList();
            foreach (User us in U)
            {
                if (us != null)
                {
                    //users.Add(us);
                    var p = client.Cypher.OptionalMatch("(user:User)-[POSTED]->(post:Post)")
                                            .Where((User user) => user.idUser == us.idUser)
                                            .AndWhere((Post post) => post.idUserPosted == us.idUser)
                                            .ReturnDistinct(post => post.As<Post>())
                                                .Results;
                    //IEnumerable<Post> Pr = p.DistinctBy;
                    List<Post> P = p.ToList();
                    foreach (Post pos in P)
                    {
                        if (pos != null)
                        {
                            UserFeed uf = new UserFeed();
                            uf.idUser = us.idUser;
                            uf.Name = us.Name;
                            uf.Surname = us.Surname;
                            uf.NickName = us.NickName;
                            uf.Avatar = us.Avatar;
                            uf.Photo = pos.Photo;
                            uf.Data = pos.Data;
                            uf.Text = pos.Text;
                            uf.Likes = pos.Likes;
                            uf.Comments = pos.Comments;
                            userfeed.Add(uf);
                            //posts.Add(pos);
                        }
                    }
                }
            }
            List<UserFeed> usrerf = userfeed.OrderByDescending((x) => x.Data).ToList();
            return View("~/Views/Home/Feed.cshtml", usrerf);
        }

        public ActionResult Delete (string photo)
        {
            var client = new GraphClient(new Uri("http://localhost:7474/db/data"));
            client.Connect();
            int id = Convert.ToInt32(Session["idUser"]);

            client.Cypher
                .OptionalMatch("(post1:Post)<-[TO]-(comment1:Comment)")
                .Where((Comment comment1) => comment1.Photo == photo)
                .AndWhere((Post post1) => post1.Photo == photo)
                .Delete("TO")
                .ExecuteWithoutResults();

            client.Cypher
                .OptionalMatch("(comment1:Comment)<-[WRITTEN]-(user1:User)")
                .Where((Comment comment1) => comment1.Photo == photo)
                .Delete("WRITTEN")
                .ExecuteWithoutResults();

            client.Cypher
                .OptionalMatch("(post1:Post)<-[POSTED]-(user1:User)")
                .Where((Post post1) => post1.Photo == photo)
                .AndWhere((Post post1) => post1.idUserPosted == id)
                .AndWhere((User user1) => user1.idUser == id)
                .Delete("POSTED")
                .ExecuteWithoutResults();

            client.Cypher
                .Match("(comment:Comment)")
                .Where((Comment comment) => comment.Photo == photo)
                .Delete("comment")
                .ExecuteWithoutResults();

            client.Cypher
                .Match("(post:Post)")
                .Where((Post post) => post.Photo == photo)
                .Delete("post")
                .ExecuteWithoutResults();

            System.IO.File.Delete("D:\\КПЗ\\Photos\\Photos\\images\\" + photo);

            int countposts = Convert.ToInt32(Session["Posts"]) - 1;
            Session.Add("Posts", countposts);

            client.Cypher
            .Match("(user:User)")
            .Where((User user) => user.idUser == id)
            .Set("user.Posts = {Posts}")
            .WithParam("Posts", countposts)
            .ExecuteWithoutResults();


            return RedirectToAction("About");
            //return View("~/Views/Home/About.cshtml", commentss);
        }
        public ActionResult DelComment(string photo, int comments, long creationtime)
        {
            var client = new GraphClient(new Uri("http://localhost:7474/db/data"));
            client.Connect();
            int id = Convert.ToInt32(Session["idUser"]);
            int oid = Convert.ToInt32(Session["oidUser"]);
            string avatar = Convert.ToString(Session["Avatar"]);
            string userdata = Convert.ToString(Session["Name"]) + " " + Convert.ToString(Session["Surname"]);

            //client.Cypher
            //    .OptionalMatch("(comment:Comment)<-[r]-()")
            //    .Where((Comment comment) => comment.CreationTime == creationtime)
            //    .AndWhere((Comment comment) => comment.idUserLeft == id)
            //    .Delete("r, comment")
            //    .ExecuteWithoutResults();

            client.Cypher
                .OptionalMatch("(post1:Post)<-[TO]-(comment1:Comment)")
                .Where((Comment comment1) => comment1.CreationTime == creationtime)
                .AndWhere((Post post1) => post1.Photo == photo)
                .Delete("TO")
                .ExecuteWithoutResults();

            var a = client.Cypher.OptionalMatch("(user:User)-[POSTED]->(post:Post)")
                        .Where((User user) => user.idUser == oid)
                        .AndWhere((Post post) => post.idUserPosted == oid)
                        .ReturnDistinct(post => post.As<Post>())
                            .Results;
            List<Post> A = a.ToList();
            int newComments = 1;
            foreach (Post pos in A)
            {
                if (pos != null)
                    newComments = pos.Comments - 1;
            } 

            client.Cypher
                .OptionalMatch("(comment1:Comment)<-[WRITTEN]-(user1:User)")
                .Where((User user1) => user1.idUser == id)
                .AndWhere((Comment comment1) => comment1.CreationTime == creationtime)
                .Delete("WRITTEN")
                .ExecuteWithoutResults();

            client.Cypher
                .Match("(comment:Comment)")
                .Where((Comment comment) => comment.CreationTime == creationtime)
                .Delete("comment")
                .ExecuteWithoutResults();


            client.Cypher
            .Match("(post:Post)")
            .Where((Post post) => post.Photo == photo)
            .Set("post.Comments = {Comments}")
            .WithParam("Comments", newComments)
            .ExecuteWithoutResults();

            var c = client.Cypher.OptionalMatch("(comment1:Comment)-[TO]->(post:Post)")
                        .Where((Post post) => post.Photo == photo)
                        .ReturnDistinct(comment1 => comment1.As<Comment>())
                            .Results;
            List<Comment> C = c.ToList();
            foreach (Comment us in C)
            {
                if (us != null)
                {
                    commentss.Add(us);
                }
            }
            ViewData["Photo"] = photo;
            ViewData["Comments"] = comments;

            return View("~/Views/Home/Comments.cshtml", commentss);
        }
        public ActionResult Comment(string comment, string photo, int comments)
        {
            var client = new GraphClient(new Uri("http://localhost:7474/db/data"));
            client.Connect();
            int id = Convert.ToInt32(Session["idUser"]);
            int oid = Convert.ToInt32(Session["oidUser"]);
            string avatar = Convert.ToString(Session["Avatar"]);
            string userdata = Convert.ToString(Session["Name"]) + " " + Convert.ToString(Session["Surname"]);
            Comment com = new Comment();
            com.Avatar = avatar;
            com.Data = DateTimeOffset.Now;
            com.idUserLeft = id;
            com.Text = comment;
            com.UserData = userdata;
            com.Photo = photo;
            com.CreationTime = DateTimeOffset.Now.UtcTicks;
            var newComment = com;
            client.Cypher
            .Create("(comment:Comment {newComment})")
            .WithParam("newComment", newComment)
            .ExecuteWithoutResults();

            client.Cypher
            .Match("(user1:User)", "(comment1:Comment)")
            .Where((User user1) => user1.idUser == id)
            .AndWhere((Comment comment1) => comment1.idUserLeft == id)
            .Create("user1-[:WRITTEN]->comment1")
            .ExecuteWithoutResults();

            client.Cypher
            .Match("(comment1:Comment)", "(post1:Post)")
            .Where((Post post1) => post1.Photo == photo)
            .AndWhere((Comment comment1) => comment1.Photo == photo)
            .Create("comment1-[:TO]->post1")
            .ExecuteWithoutResults();

            int newComments = comments + 1;

            client.Cypher
            .Match("(post:Post)")
            .Where((Post post) => post.Photo == photo)
            .Set("post.Comments = {Comments}")
            .WithParam("Comments", newComments)
            .ExecuteWithoutResults();

            var c = client.Cypher.OptionalMatch("(comment1:Comment)-[TO]->(post:Post)")
                        .Where((Post post) => post.Photo == photo)
                        .ReturnDistinct(comment1 => comment1.As<Comment>())
                            .Results;
            List<Comment> C = c.ToList();
            foreach (Comment us in C)
            {
                if (us != null)
                {
                    commentss.Add(us);
                }
            }
            ViewData["Photo"] = photo;
            ViewData["Comments"] = comments;

            return View("~/Views/Home/Comments.cshtml", commentss);
        }

        public ActionResult Comments(string photo, int comments)
        {
            var client = new GraphClient(new Uri("http://localhost:7474/db/data"));
            client.Connect();
            var c = client.Cypher.OptionalMatch("(comment1:Comment)-[TO]->(post:Post)")
                        .Where((Post post) => post.Photo == photo)
                        .ReturnDistinct(comment1 => comment1.As<Comment>())
                            .Results;
            List<Comment> C = c.ToList();
            foreach (Comment us in C)
            {
                if (us != null)
                {
                    commentss.Add(us);
                }
            }
            ViewData["Photo"] = photo;
            ViewData["Comments"] = comments;
            return View("~/Views/Home/Comments.cshtml", commentss);
        }
        public ActionResult DisLike(string photo, int likes)
        {
            var client = new GraphClient(new Uri("http://localhost:7474/db/data"));
            client.Connect();
            int id = Convert.ToInt32(Session["idUser"]);
            //client.Cypher
            //.Match("(user1:User)", "(post1:Post)")
            //.Where((User user1) => user1.idUser == id)
            //.AndWhere((Post post1) => post1.Photo == photo)
            //.Create("user1-[:LIKED]->post1")
            //.ExecuteWithoutResults();
            client.Cypher
                .OptionalMatch("(post1:Post)<-[LIKED]-(user1:User)")
                .Where((User user1) => user1.idUser == id)
                .AndWhere((Post post1) => post1.Photo == photo)
                .Delete("LIKED")
                .ExecuteWithoutResults();
            int newlikes = likes - 1;
            client.Cypher
            .Match("(post:Post)")
            .Where((Post post) => post.Photo == photo)
            .Set("post.Likes = {Likes}")
            .WithParam("Likes", newlikes)
            .ExecuteWithoutResults();
            var p = client.Cypher.OptionalMatch("(user:User)-[LIKED]->(post:Post)")
                        .Where((Post post) => post.Photo == photo)
                        .ReturnDistinct(user => user.As<User>())
                            .Results;
            List<User> P = p.ToList();
            foreach (User us in P)
            {
                if (us != null && us.idUser != Convert.ToInt32(Session["oidUser"]))
                {
                    users.Add(us);
                }
            }
            ViewData["Photo"] = photo;
            ViewData["Liked"] = false;
            //return RedirectToAction("/Likes?photo="+photo+"&likes=" + id, users);
            return RedirectToAction("Likes", new {photo=  @photo ,likes=@id});
        }
        public ActionResult Like(string photo, int likes)
        {
            var client = new GraphClient(new Uri("http://localhost:7474/db/data"));
            client.Connect();
            int id = Convert.ToInt32(Session["idUser"]);
            client.Cypher
            .Match("(user1:User)", "(post1:Post)")
            .Where((User user1) => user1.idUser == id)
            .AndWhere((Post post1) => post1.Photo == photo)
            .Create("user1-[:LIKED]->post1")
            .ExecuteWithoutResults();
            int newlikes = likes + 1;
            client.Cypher
            .Match("(post:Post)")
            .Where((Post post) => post.Photo == photo)
            .Set("post.Likes = {Likes}")
            .WithParam("Likes", newlikes)
            .ExecuteWithoutResults();
            var p = client.Cypher.OptionalMatch("(user:User)-[LIKED]->(post:Post)")
                        .Where((Post post) => post.Photo == photo)
                        .ReturnDistinct(user => user.As<User>())
                            .Results;
            List<User> P = p.ToList();
            foreach (User us in P)
            {
                if (us != null && us.idUser != Convert.ToInt32(Session["oidUser"]))
                {
                    users.Add(us);
                }
            }
            ViewData["Photo"] = photo;
            ViewData["Likes"] = likes;
            ViewData["Liked"] = true;
            //return RedirectToAction("/Likes?photo=" + photo + "&likes=" + id, users);
            return RedirectToAction("Likes", new { photo = @photo, likes= @id });
        }
        public ActionResult Likes(string photo, int likes, int iduserposted)
        {
            var client = new GraphClient(new Uri("http://localhost:7474/db/data"));
            client.Connect();
            var p = client.Cypher.OptionalMatch("(user:User)-[LIKED]->(post:Post)")
                        .Where((Post post) => post.Photo == photo)
                        .ReturnDistinct(user => user.As<User>())
                            .Results;
            List<User> P = p.ToList();
            bool liked = false;
            foreach (User us in P)
            {
                if (us != null && us.idUser != Convert.ToInt32(Session["idUser"]))
                {
                    users.Add(us);
                    if(us.idUser == Convert.ToInt32(Session["idUser"]))
                    {
                        liked = true;
                    }
                }
            }
            ViewData["Liked"] = liked;
            ViewData["Photo"] = photo;
            ViewData["Likes"] = likes;
            ViewData["idus"] = iduserposted;
            return View("~/Views/Home/Likes.cshtml", users);
        }
        public ActionResult Follow()
        {
            var client = new GraphClient(new Uri("http://localhost:7474/db/data"));
            client.Connect();
            int iduser = Convert.ToInt32(Session["oidUser"]);
            int id = Convert.ToInt32(Session["idUser"]);
            int followersnum = Convert.ToInt32(Session["oFollovers"]) + 1;
            int followingsnum = Convert.ToInt32(Session["Follovings"]) + 1;
            Session.Remove("oFollovers");
            Session.Remove("Follovings");
            Session.Add("oFollovers", followersnum);
            Session.Add("Follovings", followingsnum);
            Session.Remove("IsFollower");
            Session.Add("IsFollower", false);
            client.Cypher
                .Match("(user:User)")
                .Where((User user) => user.idUser == iduser)
                .Set("user.Follovers = {Follovers}")
                .WithParam("Follovers", followersnum)
                .ExecuteWithoutResults();
            client.Cypher
                .Match("(user:User)")
                .Where((User user) => user.idUser == id)
                .Set("user.Follovings = {Follovings}")
                .WithParam("Follovings", followersnum)
                .ExecuteWithoutResults();

            Session.Add("Follovings", followersnum);

            client.Cypher
                .Match("(user1:User)", "(user2:User)")
                .Where((User user1) => user1.idUser == id)
                .AndWhere((User user2) => user2.idUser == iduser)
                .Create("user1-[:FOLLOW]->user2")
                .ExecuteWithoutResults();
            var p = client.Cypher.OptionalMatch("(user:User)-[POSTED]->(post:Post)")
                        .Where((User user) => user.idUser == iduser)
                        .ReturnDistinct(post => post.As<Post>())
                            .Results;
            List<Post> P = p.ToList();
            foreach (Post pos in P)
            {
                if (pos != null)
                    posts.Add(pos);
            }
            return RedirectToAction("OtherPage", new { iduser = iduser });
            //return View("~/Views/Home/OtherPage?iduser="+iduser+".cshtml", posts);
        }
        public ActionResult UnFollow()
        {
            var client = new GraphClient(new Uri("http://localhost:7474/db/data"));
            client.Connect();
            int iduser = Convert.ToInt32(Session["oidUser"]);
            int id = Convert.ToInt32(Session["idUser"]);
            int followersnum = Convert.ToInt32(Session["oFollovers"]) - 1;
            int followingsnum = Convert.ToInt32(Session["Follovings"]) - 1;
            Session.Remove("oFollovers");
            Session.Remove("Follovings");
            Session.Add("oFollovers", followersnum);
            Session.Add("Follovings", followingsnum);
            Session.Remove("IsFollower");
            Session.Add("IsFollower", true);
            client.Cypher
                .Match("(user:User)")
                .Where((User user) => user.idUser == iduser)
                .Set("user.Follovers = {Follovers}")
                .WithParam("Follovers", followersnum)
                .ExecuteWithoutResults();
            client.Cypher
                .Match("(user:User)")
                .Where((User user) => user.idUser == id)
                .Set("user.Follovings = {Follovings}")
                .WithParam("Follovings", followersnum)
                .ExecuteWithoutResults();

            Session.Add("Follovings", followersnum);

            client.Cypher
                .OptionalMatch("(user1:User)<-[FOLLOW]-(user2:User)")
                .Where((User user1) => user1.idUser == iduser)
                .AndWhere((User user2) => user2.idUser == id)
                .Delete("FOLLOW")
                .ExecuteWithoutResults();
            var p = client.Cypher.OptionalMatch("(user:User)-[POSTED]->(post:Post)")
                        .Where((User user) => user.idUser == iduser)
                        .ReturnDistinct(post => post.As<Post>())
                            .Results;
            List<Post> P = p.ToList();
            foreach (Post pos in P)
            {
                if (pos != null)
                    posts.Add(pos);
            }
            return RedirectToAction("OtherPage", new { iduser = iduser });
            //return View("~/Views/Home/OtherPage?iduser=" + iduser + ".cshtml", posts);
        }
        public ActionResult OtherPage(int iduser)
        {
            if (iduser == Convert.ToInt32(Session["idUser"]))
            {
                return RedirectToAction("About");
            }
            var client = new GraphClient(new Uri("http://localhost:7474/db/data"));
            client.Connect();
            var v = client.Cypher.Match("(user:User)").Return(user => user.As<User>()).Results;
            List<User> N = v.ToList();
            foreach (User user in N)
            {
                users.Add(user);
                if (user.idUser == iduser)
                {
                    Session.Add("oidUser", user.idUser);
                    Session.Add("oName", user.Name);
                    Session.Add("oSurname", user.Surname);
                    Session.Add("oNickName", user.NickName);
                    Session.Add("oAvatar", user.Avatar);
                    Session.Add("oFollovers", user.Follovers);
                    Session.Add("oFollovings", user.Follovings);
                    Session.Add("oPosts", user.Posts);
                }
            }
            int id = Convert.ToInt32(Session["idUser"]);
            var f = client.Cypher
            .OptionalMatch("(user:User)-[FOLLOW]->(friend:User)")
                .Where((User user) => user.idUser == id)
                .AndWhere((User friend) => friend.idUser == iduser)
                .ReturnDistinct(friend => friend.As<User>())
                            .Results;
            List<User> F = f.ToList();
            bool isfollower = false;
            foreach (User us in F)
            {
                if (us != null)
                    isfollower = true;
            }
            Session.Add("IsFollower", isfollower);

            var p = client.Cypher.OptionalMatch("(user:User)-[POSTED]->(post:Post)")
                        .Where((User user) => user.idUser == iduser)
                        .AndWhere((Post post) => post.idUserPosted == iduser)
                        .ReturnDistinct(post => post.As<Post>())
                            .Results;
            List<Post> P = p.ToList();
            foreach (Post pos in P)
            {
                if (pos != null)
                    posts.Add(pos);
            }
            P = posts.OrderBy((x) => x.Data).ToList();
            return View("~/Views/Home/OtherPage.cshtml", P);
        }
        public ActionResult SearchUser(string nick)
        {
            var client = new GraphClient(new Uri("http://localhost:7474/db/data"));
            client.Connect();
            var u = client.Cypher
                .Match("(user:User)")
                .Where((User user) => user.Name == nick)
                .Return(user => user.As<User>())
                .Results;
            List<User> P = u.ToList();
            foreach (User pos in P)
            {
                if (pos != null)
                    users.Add(pos);
            }
            if (users.Count == 0)
                ViewBag.Message = "Any User with such name";
            else ViewBag.Message = "Users with such name";
            return View("~/Views/Home/Search.cshtml", users);
        }
        public ActionResult Search()
        {
            return View("~/Views/Home/Search.cshtml", users);
        }
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            var client = new GraphClient(new Uri("http://localhost:7474/db/data"));
            client.Connect();
            int id = Convert.ToInt32(Session["idUser"]);
            var p = client.Cypher.OptionalMatch("(user:User)-[POSTED]->(post:Post)")
                        .Where((User user) => user.idUser == id)
                        .AndWhere((Post post) => post.idUserPosted == id)
                        .ReturnDistinct(post => post.As<Post>())
                            .Results;
            List<Post> P = p.ToList();
            foreach (Post pos in P)
            {
                if (pos != null)
                    posts.Add(pos);
            }
            P = posts.OrderByDescending((x) => x.Data).ToList();
            return View("~/Views/Home/About.cshtml", P);
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public static bool IsAuth(string data)
        {
            string token = data;
            return token != null && token != String.Empty;
        }

        public ActionResult vk(string code)
        {
            if (!string.IsNullOrEmpty(code))
            {
                OAuth2 vk = new OAuth2("4912533", "RMuwuJroByXRlK6DbvGa", "http://api.vkontakte.ru/oauth/authorize", "https://api.vkontakte.ru/oauth/access_token", "http://localhost:51173/Home/vk");
                vk.Code = code;
                OAuth2Token token = vk.GetAccessToken(new Dictionary<string, string> { { "client_secret", "RMuwuJroByXRlK6DbvGa" } }, OAuth2.AccessTokenType.JsonDictionary);
                if (token != null)
                {
                    if (token.dictionary_token != null)
                    {
                        var client = new GraphClient(new Uri("http://localhost:7474/db/data"));
                        client.Connect();
                        var v = client.Cypher.Match("(user:User)").Return(user => user.As<User>()).Results;
                        List<User> N = v.ToList();                        
                        bool current = false;                        
                        foreach (User user in N)
                        {
                            users.Add(user);
                            if (Convert.ToInt32(token.dictionary_token["user_id"]) == user.idUser)
                            {
                                Session.Add("access_token", token.dictionary_token["access_token"]);
                                Session.Add("idUser", user.idUser);
                                Session.Add("Name", user.Name);                            
                                Session.Add("Surname", user.Surname);
                                Session.Add("NickName", user.NickName);
                                Session.Add("Avatar", user.Avatar);
                                Session.Add("Follovers", user.Follovers);
                                Session.Add("Follovings", user.Follovings);
                                Session.Add("Posts", user.Posts);
                                current = true;
                            }
                        }
                        if (current != true)
                        {                         
                            Session.Add("access_token", token.dictionary_token["access_token"]);
                            Session.Add("idUser", token.dictionary_token["user_id"]);
                            string res = OAuth2UserData.GetVKUserData(token.dictionary_token["access_token"], new Dictionary<string, string>() { 
                                        {"user_ids", token.dictionary_token["user_id"]},
                                        {"fields", "photo_200, screen_name"}});
                            res = res.Replace("\"", "'");
                            res = res.Replace("{'response':[", "");
                            res = res.Replace("}]}", "}");
                            dynamic stuff = JObject.Parse(res);
                            string first_name = stuff.first_name;
                            Session.Add("Name", first_name);
                            string last_name = stuff.last_name;
                            Session.Add("Surname", last_name);
                            string screen_name = stuff.screen_name;
                            Session.Add("NickName", screen_name);
                            string photo_id = stuff.photo_200;
                            Session.Add("Avatar", photo_id);
                            Session.Add("Follovers", 0);
                            Session.Add("Follovings", 0);
                            Session.Add("Posts", 0);
                            User user = new User();
                            user.idUser = Convert.ToInt32(token.dictionary_token["user_id"]);
                            user.Name = stuff.first_name;
                            user.Surname = stuff.last_name;
                            user.NickName = stuff.screen_name;
                            user.Avatar = stuff.photo_200;
                            user.Follovers = 0;
                            user.Follovings = 0;
                            user.Posts = 0;
                            var newUser = user;
                            client.Cypher
                            .Create("(user:User {newUser})")
                            .WithParam("newUser", newUser)
                            .ExecuteWithoutResults();
                        }
                        else
                        {
                            int id = Convert.ToInt32(Session["idUser"]);
                            var p = client.Cypher.OptionalMatch("(user:User)-[POSTED]->(post:Post)")
                                        .Where((User user) => user.idUser == id)
                                        .ReturnDistinct(post => post.As<Post>())
                                            .Results;
                            //IEnumerable<Post> Pr = p.DistinctBy;
                            List<Post> P = p.ToList();
                            foreach (Post pos in P)
                            {
                                posts.Add(pos);
                            }
                        }
                    }
                }
            }
            else
            {
                OAuth2 vk = new OAuth2("4912533", "RMuwuJroByXRlK6DbvGa", "http://api.vkontakte.ru/oauth/authorize", "https://api.vkontakte.ru/oauth/access_token", "http://localhost:51173/Home/vk");
                vk.GetAuthCode(new Dictionary<string, string>() { { "display", "popup" } });
            }
            return RedirectToAction("About");
        }

        public ActionResult newPost()
        {
            var client = new GraphClient(new Uri("http://localhost:7474/db/data"));
            client.Connect();

            int id = Convert.ToInt32(Session["idUser"]);
            

            Post npost = new Post();
            npost.idUserPosted = Convert.ToInt32(Session["idUser"]);
            npost.Photo = Convert.ToString(Session["imagePath"]);
            Session.Remove("imagePath");
            npost.Text = Convert.ToString(Session["com"]);
            Session.Remove("com");
            npost.Data = DateTimeOffset.Now;
            npost.Likes = 0;
            npost.Comments = 0;

            var newPost = npost;
            client.Cypher
            .Create("(post:Post {newPost})")
            .WithParam("newPost", newPost)
            .ExecuteWithoutResults();

            int iduser = Convert.ToInt32(Session["idUser"]);

            client.Cypher
            .Match("(user1:User)", "(post1:Post)")
            .Where((User user1) => npost.idUserPosted == user1.idUser)
            .AndWhere((Post post1) => post1.idUserPosted == iduser)
            .Create("user1-[:POSTED]->post1")
            .ExecuteWithoutResults();

            int countposts = Convert.ToInt32(Session["Posts"]) + 1;

            client.Cypher
            .Match("(user:User)")
            .Where((User user) => user.idUser == iduser)
            .Set("user.Posts = {Posts}")
            .WithParam("Posts", countposts)
            .ExecuteWithoutResults();
            
            Session.Remove("Posts");
            Session.Add("Posts", countposts);

            return RedirectToAction("About");
            //return View("~/Views/Home/About.cshtml", posts);
        }
    }
}