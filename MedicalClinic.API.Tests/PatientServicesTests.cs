using MedicalClinic.Application.Dtos.PatientDto;
using MedicalClinic.Application.Dtos.PatientResponseDto;
using MedicalClinic.Application.Exceptions;
using MedicalClinic.Application.Interfaces;
using MedicalClinic.Application.Services;
using MedicalClinic.Domain.Entities;
using Xunit;
using Moq;


namespace MedicalClinic.API.Tests
{
    public class PatientServicesTests
    {
        private readonly Mock<IUnitOfWork> _uow = new();
        private readonly Mock<IPatientRepository> _patients = new();
        private readonly PatientServices _service;
        public PatientServicesTests()
        {
            _uow.Setup(p => p.Patients).Returns(_patients.Object);
              _service =new PatientServices( _uow.Object);
        }
        [Fact]
        public async Task GetByIdAsync_WhenPatientReadsOwnRecord_ReturnsDto()
        {
            //Arrange
            _patients.Setup(p => p.GetByIdAsync(1)).ReturnsAsync(new Patient { Id = 1, FirstName = "Samer", IdentityUserId = "user-1" });
            //Act
            var result =  await _service.GetByIdAsync(1, "user-1", false);
            //Assert
            Assert.Equal(1, result.Id);
            Assert.Equal("Samer", result.FirstName);

        }

        [Fact]
        public async Task GetByIdAsync_WhenStaffReadsAnyPatient_ReturnsDto()
        {
            _patients.Setup(p => p.GetByIdAsync(1))
                     .ReturnsAsync(new Patient { Id = 1, FirstName = "Sara", IdentityUserId = "user-1" });

            // a doctor/admin account (different from the owner), isStaff = true
            var result = await _service.GetByIdAsync(1, "doctor-account", true);

            Assert.Equal("Sara", result.FirstName);
        }
        [Fact]
        public async Task GetByIdAsync_WhenPatientReadsAnotherPatient_ThrowsForbiddenException()
        {
            _patients.Setup(p => p.GetByIdAsync(1)).ReturnsAsync(new Patient { Id = 1, FirstName = "Samer", IdentityUserId = "user1" });

            await Assert.ThrowsAsync<ForbiddenException>(() => (_service.GetByIdAsync(1, "SomeOneElse", false)));

        }
        [Fact]
        public async Task GetByIdAsync_WhenPatientMissing_ThrowsNotFoundException()
        {
            _patients.Setup(p => p.GetByIdAsync(99)).ReturnsAsync((Patient?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.GetByIdAsync(99, "user-1", false));
        }
        //end of GetById test
        [Fact]
        public async Task GetMeAsync_WhenProfileExists_ReturnsDto()
        {
            _patients.Setup(p => p.GetByIdentityUserIdAsync("user-1")).ReturnsAsync(new Patient { Id=1,FirstName="Samer"});
            var result = await  _service.GetMeAsync("user-1");
            Assert.Equal(1, result.Id);
            Assert.Equal("Samer", result.FirstName);
        }
        [Fact]
        public async Task GetMeAsync_WhenProfileMissing_ThrowsNotFoundException()
        {
            _patients.Setup(p => p.GetByIdentityUserIdAsync("user-1")).ReturnsAsync((Patient?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.GetMeAsync("user-1"));
        }
        //end of GetMe test
        [Fact]
        public async Task UpdateAsync_WhenOwnerUpdates_ChangesFields_AndSaves()
        {
            var patient = new Patient {Id=1,FirstName="Old", IdentityUserId="user-1"};
            _patients.Setup(p => p.GetByIdAsync(1)).ReturnsAsync(patient);
            await _service.UpdateAsync(1, new UpdatePatientDto { FirstName = "New" }, "user-1", false);
            Assert.Equal(1, patient.Id);
            Assert.Equal("New", patient.FirstName);
            _patients.Verify(u => u.Update(patient), Times.Once);
            _uow.Verify(u => u.CompleteAsync(), Times.Once);
        }
        [Fact]
      
        public async Task UpdateAsync_WhenAdminUpdatesAnotherPatient_ChangesFields_AndSaves()
        {
            var patient = new Patient { Id = 1, FirstName = "Old", IdentityUserId = "user-1" };
            _patients.Setup(p => p.GetByIdAsync(1)).ReturnsAsync(patient);

            await _service.UpdateAsync(1, new UpdatePatientDto { FirstName = "New" }, "admin-account", true);

            Assert.Equal("New", patient.FirstName);
            _patients.Verify(u => u.Update(patient), Times.Once);
            _uow.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_WhenStranger_ThrowsForbiddenException_AndDoesNotSave()
        {
            var patient = new Patient { Id = 1, FirstName = "Old", IdentityUserId = "user-1" };
            _patients.Setup(p => p.GetByIdAsync(1)).ReturnsAsync(patient);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                _service.UpdateAsync(1, new UpdatePatientDto { FirstName = "Hacked" }, "someone-else", false));

            Assert.Equal("Old", patient.FirstName);
            _uow.Verify(u => u.CompleteAsync(), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_WhenPatientMissing_ThrowsNotFoundException_AndDoesNotSave()
        {
            _patients.Setup(p => p.GetByIdAsync(99)).ReturnsAsync((Patient?)null);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                _service.UpdateAsync(99, new UpdatePatientDto(), "user-1", false));
     
            _uow.Verify(u => u.CompleteAsync(), Times.Never);
        }
        //End of Update test
        [Fact]
        public async Task DeleteAsync_WhenPatientExists_DeletesAndSaves()
        {
            var patient = new Patient { Id = 1 };
            _patients.Setup(p => p.GetByIdAsync(1)).ReturnsAsync(patient);

            await _service.DeleteAsync(1);

            _patients.Verify(p => p.Delete(patient), Times.Once);
            _uow.Verify(u => u.CompleteAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_WhenPatientMissing_ThrowsNotFoundException_AndDoesNotSave()
        {
            _patients.Setup(p => p.GetByIdAsync(99)).ReturnsAsync((Patient?)null);

            await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(99));

            _uow.Verify(u => u.CompleteAsync(), Times.Never);
        }
        //End of Delete test
        [Fact]
        public async Task SearchByNameAsync_WhenNoMatches_ThrowsNotFoundException()
        {
            _patients.Setup(p => p.GetPatientsByNameAsync("zzz")).ReturnsAsync(new List<Patient>());

            await Assert.ThrowsAsync<NotFoundException>(() => _service.SearchByNameAsync("zzz"));
        }

        [Fact]
        public async Task SearchByNameAsync_WhenMatches_ReturnsMappedDtos()
        {
            _patients.Setup(p => p.GetPatientsByNameAsync("Sa")).ReturnsAsync(new List<Patient>
        {
            new() { Id = 1, FirstName = "Sara" },
            new() { Id = 2, FirstName = "Samer" }
        });

            var result = (await _service.SearchByNameAsync("Sa")).ToList();

            Assert.Equal(2, result.Count);
            Assert.Equal("Sara", result[0].FirstName);
        }
        // end of  SearchByName test
        [Theory]
        [InlineData(0, 0, 1, 10)]    // both invalid -> defaults
        [InlineData(-5, -1, 1, 10)]  // negative -> defaults
        [InlineData(3, 20, 3, 20)]   // valid values stay as they are
        public async Task GetAllAsync_NormalizesPagingValues(int pageNumber, int pageSize, int expectedPage, int expectedSize)
        {
            _patients.Setup(p => p.GetPagedPatientsAsync(It.IsAny<int>(), It.IsAny<int>()))
                     .ReturnsAsync(new List<Patient>());
            _patients.Setup(p => p.GetTotalCountAsync()).ReturnsAsync(0);

            var result = await _service.GetAllAsync(pageNumber, pageSize);

            Assert.Equal(expectedPage, result.PageNumber);
            Assert.Equal(expectedSize, result.PageSize);
        }
    }
}
