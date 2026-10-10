using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace MedicalClinic.Application.Dtos.AppointmentsResponedDto
{
    public class CreateAppointmentDto
    {
        [Required(ErrorMessage = "معرف الطبيب مطلوب")]
        public int DoctorId { get; set; }

        [Required(ErrorMessage = "تاريخ ووقت الموعد مطلوب")]
        public DateTime AppointmentDate { get; set; }

        public string Notes { get; set; } = string.Empty;
    }
}