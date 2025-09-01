using Microsoft.AspNetCore.Mvc;
using SchoolManagement.API.DTOs;
using SchoolManagement.API.Interfaces;
using SchoolManagement.API.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SchoolManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TeacherController(ITeacherService teacherService) : ControllerBase
    {
        private readonly ITeacherService _teacherService = teacherService;


        // GET: api/<ValuesController>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TeacherDto>>> GetAllTeachers(int userId)
        {
            IEnumerable<TeacherDto> teachers = await _teacherService.GetTeachersAsync(userId);

            if (teachers == null || !teachers.Any()) return NoContent();

            return Ok(teachers);
        }

        // GET api/<ValuesController>/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TeacherDto>> GetTeacher(int id, int userId)
        {
            TeacherDto teacher = await _teacherService.GetTeacherByIdAsync(id, userId);

            return Ok(teacher);
        }

        [HttpGet("by-user/{userId}")]
        public async Task<ActionResult<TeacherInputDto>> GetTeacherByUserId(int userId)
        {
            TeacherInputDto teacher = await _teacherService.GetTeacherByUserIdAsync(userId);

            return Ok(teacher);
        }

        // POST api/<ValuesController>
        [HttpPost]
        public async Task<ActionResult<TeacherInputDto>> PostTeacher(TeacherInputDto teacherToBeCreated, int userId)
        {
            TeacherInputDto createdTeacher = await _teacherService.CreateTeacherAsync(teacherToBeCreated, userId);

            return CreatedAtAction("GetTeacher", new { id = createdTeacher.Id }, createdTeacher);
        }

        // PUT api/<ValuesController>/5
        [HttpPut("{id}")]
        public async Task<ActionResult<TeacherInputDto>> PutTeacher(int id, TeacherInputDto teacherToBeUpdated, int userId)
        {
            TeacherInputDto updatedTeacher = await _teacherService.UpdateTeacherAsync(id, teacherToBeUpdated, userId);

            return Ok(updatedTeacher);
        }

        // DELETE api/<ValuesController>/5
        [HttpDelete("{id}")]
        public async Task<ActionResult<bool>> DeleteTeacher(int id, int userId)
        {
            await _teacherService.DeleteTeacherAsync(id, userId); 
            
            return NoContent();
        }
    }
}
