using DocumentFormat.OpenXml.Drawing.Charts;
using ErrorOr;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TEMApps.Common.Common.Common.Helpers;
using TEMApps.DTOs.Booking;
using TEMApps.Interfaces;
using TEMApps.Services.Services;
using TEMPApps.DTOs.ReSchedulingDto;

namespace TEMApps.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReScheduleController : ControllerBase
    {
        #region VARIABLES
        private readonly IReScheduleService _reScheduleService;
        #endregion

        #region CONSTRUCTOR
        public ReScheduleController(IReScheduleService reScheduleService)
        {
            _reScheduleService = reScheduleService;
        }
        #endregion
        #region API

        //[HttpGet("{id}/details")]
        //[Authorize]
        //public async Task<ActionResult<ReScheduleTicketDto>> GetDetails(Guid id)
        //{
        //    var userId = User.GetUserId();
        //    var details = await _reScheduleService.GetBookingDetailsAsync(id, userId);
        //    if (details == null) return Ok("Booking not found or access denied.");
        //    return Ok(details);
        //}
        [HttpPost]
        public async Task<IActionResult> RescheduleTicket([FromBody] ReScheduleTicketDto dto)
        {
            //if(dto==null)
            //    return BadRequest("Request Body cannot be null");
            //    if (dto.TicketId == Guid.Empty)
            //        return BadRequest("Invalid TicketId");
            //    if (dto.NewDate <= DateTimeOffset.UtcNow)
            //        return BadRequest("Cannot reschedule to a past date");
            //    var result = await _reScheduleService.RescheduleTicketAsync(dto);
            //    return Ok(new
            //    {Success=true,
            //    Data=result});

            try
            {
                var result = await _reScheduleService.RescheduleTicketAsync(dto);
                return Ok(result);
            }

            catch (Exception ex)
            {
                // Log the exception (not implemented here)
                return BadRequest(new
                {
                    Message = ex.Message,
                    Inner = ex.InnerException?.Message
                });
            }
        }

        }
    #endregion
}

