using MedicalClinic.Application.Dtos.DoctorResponseDto;
using MedicalClinic.Application.Exceptions;
using MedicalClinic.Application.Interfaces;
using MedicalClinic.Application.Services;
using MedicalClinic.Domain.Entities;
using Moq;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace MedicalClinic.API.Tests
{
    public class DoctorServicesTests
    {
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<IDoctorRepository> _doctor = new();
        private readonly DoctorServices _service;
        public DoctorServicesTests()
        {
            _uow.Setup(d => d.Doctors).Returns(_doctor.Object);
            _service = new DoctorServices(_uow.Object);
        }
        [Fact]
        public async Task GetByIdAsync_WhenDoctorExsist_ReturnsDto()
        {
            //Arrange
            _doctor.Setup(d => d.GetByIdAsync(1)).ReturnsAsync(new Doctor { Id = 1, FirstName = "dr.Samer" });
            //Act
            var result = await _service.GetDoctorByIdAsync(1);
            //Assert
            Assert.Equal(1, result.Id);
            Assert.Equal("dr.Samer", result.FirstName);
        }
        [Fact]
        public async Task GetByIdAsync_WhenDoctorNotExsist_ThrowNotFoundException()
        {
            //Arrange
            _doctor.Setup(d => d.GetByIdAsync(99)).ReturnsAsync((Doctor?)null);
            //Act
            await Assert.ThrowsAsync<NotFoundException>(() => _service.GetDoctorByIdAsync(99));

        }
        //End of GetbyId Tsets
        [Fact]
        public async Task GetallDoctor_ReturnAlldoctor()
        {
            _doctor.Setup(d => d.GetAllAsync()).ReturnsAsync(new List<Doctor> {
                new Doctor{Id=1,FirstName="Dr.Samer"},
                new Doctor{Id=2,FirstName="Dr.Majdi" }

            });
            var results = (await _service.GetallDoctorAsync()).ToList();
            Assert.Equal(2, results.Count);
            Assert.Equal("Dr.Samer", results[0].FirstName);
        }
        //End of GetAllDoctor Test
        [Fact]
        public async Task GetDoctorBySpecializationAsync_ReturnsMatchingDoctorsAsDtos()
        {
            _doctor.Setup(d => d.GetDoctorBySpecializationAsync("Cardiology")).ReturnsAsync(new List<Doctor>
        {
            new() { Id = 1, FirstName = "Ali", Specialization = "Cardiology" },
            new() { Id = 2, FirstName = "Omar", Specialization = "Cardiology" }
        });

            var result = (await _service.GetDoctorBySpecializationAsync("Cardiology")).ToList();

            Assert.Equal(2, result.Count);
            Assert.All(result, d => Assert.Equal("Cardiology", d.Specialization));
        }
        // End of GetDoctorBySpecialization test


        [Fact]
        public async Task ToggleDoctorActiveAsync_WhenDoctorMissing_ThrowsNotFoundException_AndDoesNotSave()
        {
            _doctor.Setup(d => d.GetByIdAsync(99)).ReturnsAsync((Doctor?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.ToggleDoctorActiveAsync(99));

            _uow.Verify(u => u.CompleteAsync(), Times.Never);
        }

        [Theory]
        [InlineData(true, false)]   // active   -> becomes inactive
        [InlineData(false, true)]   // inactive -> becomes active
        public async Task ToggleDoctorActiveAsync_FlipsIsActive_AndSaves(bool before, bool after)
        {
            var doctor = new Doctor { Id = 1, IsActive = before };
            _doctor.Setup(d => d.GetByIdAsync(1)).ReturnsAsync(doctor);

            await _service.ToggleDoctorActiveAsync(1);

            Assert.Equal(after, doctor.IsActive);
            _doctor.Verify(d => d.Update(doctor), Times.Once);
            _uow.Verify(u => u.CompleteAsync(), Times.Once);
        }
        // End Of toogle Tset
        [Fact]
        public async Task UpdateDoctorAsync_UpdatesFields_AndSaves()
        {
            var doctor = new Doctor { Id = 1, FirstName = "Old", LastName = "Name" };
            _doctor.Setup(d => d.GetByIdAsync(1)).ReturnsAsync(doctor);

            await _service.UpdateDoctorAsync(1, new UpdateDoctorDto { FirstName = "New", LastName = "Doctor" });

            Assert.Equal("New", doctor.FirstName);
            Assert.Equal("Doctor", doctor.LastName);
            _doctor.Verify(d => d.Update(doctor), Times.Once);
            _uow.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateDoctorAsync_WhenDoctorMissing_ThrowsNotFoundException_AndDoesNotSave()
        {
            _doctor.Setup(d => d.GetByIdAsync(99)).ReturnsAsync((Doctor?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.UpdateDoctorAsync(99, new UpdateDoctorDto()));

            _uow.Verify(u => u.CompleteAsync(), Times.Never);
        }
        //End of updateDoctor tests


        [Fact]
        public async Task DeleteDoctorAsync_WhenDoctorExists_DeletesAndSaves()
        {
            var doctor = new Doctor { Id = 1 };
            _doctor.Setup(d => d.GetByIdAsync(1)).ReturnsAsync(doctor);

            await _service.DeleteDoctorAsync(1);

            _doctor.Verify(d => d.Delete(doctor), Times.Once);
            _uow.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteDoctorAsync_WhenDoctorMissing_ThrowsNotFoundException_AndDoesNotSave()
        {
            _doctor.Setup(d => d.GetByIdAsync(99)).ReturnsAsync((Doctor?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteDoctorAsync(99));

            _uow.Verify(u => u.CompleteAsync(), Times.Never);
        }
    }
}
