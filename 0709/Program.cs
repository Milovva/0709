using _0709.Middleware;
using System.Text;

namespace _0709
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddSingleton<IUserRepository, UserRepository>();

            var app = builder.Build();

            // ƒобавл€ем ErrorHandlingMiddleware
            app.UseMiddleware<ErrorHandlingMiddleware>();

            // √лавна€ страница с отображением всех пользователей и ссылками на действи€
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/")
                {
                    var repository = context.RequestServices.GetService<IUserRepository>();
                    var users = repository.GetAll();

                    var response = new StringBuilder();
                    response.Append("<h1>User Management</h1>");
                    response.Append("<h2>Users List</h2>");
                    response.Append("<ul>");
                    foreach (var user in users)
                    {
                        response.Append($"<li>{user.Name} - {user.Email} " +
                                        $"<a href=\"/edit?id={user.Id}\">Edit</a> | " +
                                        $"<a href=\"/delete?id={user.Id}\">Delete</a></li>");
                    }
                    response.Append("</ul>");
                    response.Append("<a href=\"/add\">Add New User</a>");

                    context.Response.ContentType = "text/html; charset=utf-8";
                    await context.Response.WriteAsync(response.ToString());
                }
                else
                {
                    await next();
                }
            });

            // ‘орма дл€ добавлени€ нового пользовател€
            app.MapWhen(context => context.Request.Path == "/add", appBuilder =>
            {
                appBuilder.Run(async context =>
                {
                    if (context.Request.Method == "GET")
                    {
                        context.Response.ContentType = "text/html; charset=utf-8";
                        await context.Response.WriteAsync(@"
                            <h2>Add User</h2>
                            <form method='post' action='/add'>
                                Name: <input type='text' name='name' required /><br>
                                Email: <input type='email' name='email' required /><br>
                                <button type='submit'>Add User</button>
                            </form>");
                    }
                    else if (context.Request.Method == "POST")
                    {
                        var repository = context.RequestServices.GetService<IUserRepository>();
                        var name = context.Request.Form["name"];
                        var email = context.Request.Form["email"];

                        if (!string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(email))
                        {
                            repository.Add(new User { Name = name, Email = email });
                            context.Response.Redirect("/");
                        }
                        else
                        {
                            context.Response.StatusCode = 400;
                            await context.Response.WriteAsync("Name and Email are required.");
                        }
                    }
                });
            });

            // ‘орма дл€ редактировани€ пользовател€
            app.MapWhen(context => context.Request.Path == "/edit", appBuilder =>
            {
                appBuilder.Run(async context =>
                {
                    var repository = context.RequestServices.GetService<IUserRepository>();
                    if (context.Request.Method == "GET" && int.TryParse(context.Request.Query["id"], out int id))
                    {
                        var user = repository.Get(id);
                        if (user != null)
                        {
                            context.Response.ContentType = "text/html; charset=utf-8";
                            await context.Response.WriteAsync($@"
                                <h2>Edit User</h2>
                                <form method='post' action='/edit?id={id}'>
                                    Name: <input type='text' name='name' value='{user.Name}' required /><br>
                                    Email: <input type='email' name='email' value='{user.Email}' required /><br>
                                    <button type='submit'>Update User</button>
                                </form>");
                        }
                        else
                        {
                            context.Response.StatusCode = 404;
                            await context.Response.WriteAsync("User not found");
                        }
                    }
                    else if (context.Request.Method == "POST" && int.TryParse(context.Request.Query["id"], out id))
                    {
                        var user = repository.Get(id);
                        if (user != null)
                        {
                            user.Name = context.Request.Form["name"];
                            user.Email = context.Request.Form["email"];
                            repository.Update(user);
                            context.Response.Redirect("/");
                        }
                        else
                        {
                            context.Response.StatusCode = 404;
                            await context.Response.WriteAsync("User not found");
                        }
                    }
                });
            });

            // ”даление пользовател€
            app.MapWhen(context => context.Request.Path == "/delete", appBuilder =>
            {
                appBuilder.Run(async context =>
                {
                    if (int.TryParse(context.Request.Query["id"], out int id))
                    {
                        var repository = context.RequestServices.GetService<IUserRepository>();
                        if (repository.Get(id) != null)
                        {
                            repository.Remove(id);
                            context.Response.Redirect("/");
                        }
                        else
                        {
                            context.Response.StatusCode = 404;
                            await context.Response.WriteAsync("User not found");
                        }
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        await context.Response.WriteAsync("Invalid user ID");
                    }
                });
            });

            app.Run();
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
    }

    public interface IUserRepository
    {
        void Add(User user);
        void Remove(int id);
        User Get(int id);
        IEnumerable<User> GetAll();
        void Update(User user);
    }

    public class UserRepository : IUserRepository
    {
        private readonly List<User> _users = new List<User>();
        private int _nextId = 1;

        public void Add(User user)
        {
            user.Id = _nextId++;
            _users.Add(user);
        }

        public IEnumerable<User> GetAll()
        {
            return _users; // ”бедитесь, что здесь только одно определение
        }

        public void Remove(int id)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                _users.Remove(user);
            }
        }

        public User Get(int id)
        {
            return _users.FirstOrDefault(u => u.Id == id);
        }

        public void Update(User user)
        {
            var existingUser = _users.FirstOrDefault(u => u.Id == user.Id);
            if (existingUser != null)
            {
                existingUser.Name = user.Name;
                existingUser.Email = user.Email;
            }
        }
    }
}
