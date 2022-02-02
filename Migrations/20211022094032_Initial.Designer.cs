﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using NjuCsCmsHelper.Models;

#nullable disable

namespace NjuCsCmsHelper.Server.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20211022094032_Initial")]
    partial class Initial
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.0-rc.2.21480.5");

            modelBuilder.Entity("NjuCsCmsHelper.Models.Assignment", b => {
                b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");

                b.Property<long>("Deadline").HasColumnType("INTEGER");

                b.Property<int>("NumberOfProblems").HasColumnType("INTEGER");

                b.HasKey("Id");

                b.ToTable("Assignments");
            });

            modelBuilder.Entity("NjuCsCmsHelper.Models.Mistake", b => {
                b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");

                b.Property<int>("AssignmentId").HasColumnType("INTEGER");

                b.Property<int?>("CorrectedInId").HasColumnType("INTEGER");

                b.Property<int>("MakedInId").HasColumnType("INTEGER");

                b.Property<int>("ProblemId").HasColumnType("INTEGER");

                b.Property<int>("StudentId").HasColumnType("INTEGER");

                b.HasKey("Id");

                b.HasIndex("AssignmentId");

                b.HasIndex("CorrectedInId");

                b.HasIndex("MakedInId");

                b.HasIndex("StudentId");

                b.ToTable("Mistakes");
            });

            modelBuilder.Entity("NjuCsCmsHelper.Models.Student", b => {
                b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");

                b.Property<string>("Name").IsRequired().HasColumnType("TEXT");

                b.Property<int>("ReviewerId").HasColumnType("INTEGER");

                b.HasKey("Id");

                b.ToTable("Students");
            });

            modelBuilder.Entity("NjuCsCmsHelper.Models.Submission", b => {
                b.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");

                b.Property<int>("AssignmentId").HasColumnType("INTEGER");

                b.Property<string>("Comment").IsRequired().HasColumnType("TEXT");

                b.Property<int>("Grade").HasColumnType("INTEGER");

                b.Property<int>("StudentId").HasColumnType("INTEGER");

                b.Property<long>("SubmittedAt").HasColumnType("INTEGER");

                b.Property<string>("Track").IsRequired().HasColumnType("TEXT");

                b.HasKey("Id");

                b.HasIndex("AssignmentId");

                b.HasIndex("StudentId");

                b.ToTable("Submissions");
            });

            modelBuilder.Entity("NjuCsCmsHelper.Models.Mistake", b => {
                b.HasOne("NjuCsCmsHelper.Models.Assignment", "Assignment")
                    .WithMany()
                    .HasForeignKey("AssignmentId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.HasOne("NjuCsCmsHelper.Models.Submission", "CorrectedIn")
                    .WithMany("HasCorrected")
                    .HasForeignKey("CorrectedInId");

                b.HasOne("NjuCsCmsHelper.Models.Submission", "MakedIn")
                    .WithMany("NeedCorrection")
                    .HasForeignKey("MakedInId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.HasOne("NjuCsCmsHelper.Models.Student", "Student")
                    .WithMany()
                    .HasForeignKey("StudentId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("Assignment");

                b.Navigation("CorrectedIn");

                b.Navigation("MakedIn");

                b.Navigation("Student");
            });

            modelBuilder.Entity("NjuCsCmsHelper.Models.Submission", b => {
                b.HasOne("NjuCsCmsHelper.Models.Assignment", "Assignment")
                    .WithMany()
                    .HasForeignKey("AssignmentId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.HasOne("NjuCsCmsHelper.Models.Student", "Student")
                    .WithMany("Submissions")
                    .HasForeignKey("StudentId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.Navigation("Assignment");

                b.Navigation("Student");
            });

            modelBuilder.Entity("NjuCsCmsHelper.Models.Student", b => { b.Navigation("Submissions"); });

            modelBuilder.Entity("NjuCsCmsHelper.Models.Submission", b => {
                b.Navigation("HasCorrected");

                b.Navigation("NeedCorrection");
            });
#pragma warning restore 612, 618
        }
    }
}
