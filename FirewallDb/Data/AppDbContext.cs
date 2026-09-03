using FirewallDb.Models;
using Microsoft.EntityFrameworkCore;

namespace FirewallDb.Data;

public class AppDbContext : DbContext 
    
    // наследуется встроенный класс из билиотеки ентити (для работы с бд)
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) //басе передает в родительский класс
    {
    } // конструктор для передачи настроек подключения, они в аппсетингс то есть это коннектион стринг для бд
    public DbSet<Rule> Rules { get; set; } // коллекция для таблицы объектов типа руле в базе данных
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Rule>()
            .HasIndex(r => r.Priority);
    }
}