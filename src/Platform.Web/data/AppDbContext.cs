using Microsoft.EntityFrameworkCore;
using Platform.Web.Models;

namespace Platform.Web.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<TaskGroup> TaskGroups => Set<TaskGroup>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<TaskAssignment> TaskAssignments => Set<TaskAssignment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasOne(department => department.Manager)
                .WithMany()
                .HasForeignKey(department => department.ManagerId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Employee>(entity =>
        {
            entity.HasIndex(employee => employee.Email)
                .IsUnique();

            entity.HasOne(employee => employee.Department)
                .WithMany(department => department.Employees)
                .HasForeignKey(employee => employee.DepartmentId)
                .OnDelete(DeleteBehavior.NoAction);

            entity.HasOne(employee => employee.Role)
                .WithMany(role => role.Employees)
                .HasForeignKey(employee => employee.RoleId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasData(
                new Role { Id = Role.AdminId, Name = "Admin" },
                new Role { Id = Role.EmployeeId, Name = "Çalışan" },
                new Role { Id = Role.ManagerId, Name = "Yönetici" }
            );
        });

        modelBuilder.Entity<TaskGroup>(entity =>
        {
            entity.HasOne(group => group.CreatedBy)
                .WithMany()
                .HasForeignKey(group => group.CreatedById)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<GroupMember>(entity =>
        {
            entity.HasIndex(member => new { member.GroupId, member.EmployeeId })
                .IsUnique();

            entity.HasOne(member => member.Group)
                .WithMany(group => group.Members)
                .HasForeignKey(member => member.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(member => member.Employee)
                .WithMany()
                .HasForeignKey(member => member.EmployeeId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasOne(task => task.Group)
                .WithMany(group => group.Tasks)
                .HasForeignKey(task => task.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(task => task.CreatedBy)
                .WithMany()
                .HasForeignKey(task => task.CreatedById)
                .OnDelete(DeleteBehavior.NoAction);
        });

        modelBuilder.Entity<TaskAssignment>(entity =>
        {
            entity.HasIndex(assignment => new { assignment.TaskId, assignment.EmployeeId })
                .IsUnique();

            entity.HasOne(assignment => assignment.TaskItem)
                .WithMany(task => task.Assignments)
                .HasForeignKey(assignment => assignment.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(assignment => assignment.Employee)
                .WithMany()
                .HasForeignKey(assignment => assignment.EmployeeId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }
}