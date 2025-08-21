using SchoolManagement.API.DTOs;
using SchoolManagement.API.Models;

namespace SchoolManagement.API.Interfaces
{
    public interface IStudentService
    {
        Task<IEnumerable<StudentResponseDto>> GetStudentsAsync();
        Task<StudentResponseDto> GetStudentByIdAsync(int id);
        Task<StudentInputDto> CreateStudentAsync(StudentInputDto studentToBeCreated);
        Task<StudentInputDto> UpdateStudentAsync(int id, StudentInputDto studentToBeUpdated);
        Task<bool> DeleteStudentAsync(int id);
    }
}
