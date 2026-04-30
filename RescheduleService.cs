using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TEMApps.Interfaces;
using TEMApps.Models.Models;
using TEMApps.Data;
using TEMPApps.DTOs.ReSchedulingDto;
using TEMPApps.Models.Models;
using TEMApps.Interfaces;


namespace TEMApps.Services.Services
{
    public class RescheduleService : IReScheduleService

    {
        private readonly ApplicationDbContext _context;
        public RescheduleService(ApplicationDbContext context)
        {
            _context = context;
        }

        public Task GetBookingDetailsAsync(Guid id, Guid userId)
        {
            throw new NotImplementedException();
        }

        //public async Task<ReScheduleResponseDto> RescheduleTicketAsync(ReScheduleTicketDto dto)
        //{
        //    var NewDateUTC=dto.NewDate.UtcDateTime;
        //    var ticket = await _context.Bookings.FindAsync(dto.TicketId);
        //    if (ticket == null)
        //    {
        //        throw new Exception("Ticket not found");
        //    }
        //    if (NewDateUTC < DateTime.UtcNow)
        //    {
        //        throw new Exception("Cannot reschedule to past date");
        //    }

        //    var reshedule = new RescheduleTicket
        //    {
        //        Id=Guid.NewGuid(),
        //        TicketId = ticket.Id,
        //        OldDate = DateTime.SpecifyKind(ticket.BookingDate, DateTimeKind.Utc),
        //        NewDate = NewDateUTC,
        //        Reason = dto.Reason
        //    };
        //    // update the ticket with new date
        //    ticket.BookingDate = NewDateUTC;
        //    _context.RescheduleTickets.Add(reshedule);
        //    await _context.SaveChangesAsync();
        //    return new ReScheduleResponseDto
        //    {
        //        Id = reshedule.Id,
        //        TicketId = reshedule.TicketId,
        //        OldDate = reshedule.OldDate,
        //        NewDate = reshedule.NewDate,
        //        Message = "Ticket rescheduled successfully"
        //    };
        //}
        public async Task<ReScheduleResponseDto> RescheduleTicketAsync(ReScheduleTicketDto dto)
        {
            var newDateUtcOffset = dto.NewDate.ToUniversalTime();
            var newBookingDate = DateTime.SpecifyKind(newDateUtcOffset.UtcDateTime, DateTimeKind.Unspecified);
            

            var ticket = await _context.Bookings.FindAsync(dto.TicketId);

            if (ticket == null)

            {

                throw new Exception("Ticket not found");

            }

            var currentTimeUtcOffset = DateTimeOffset.UtcNow;

            if (newDateUtcOffset < currentTimeUtcOffset)
            {
                throw new Exception("Cannot reschedule to past date");
            }

            var reschedule = new RescheduleTicket
            {
                Id = Guid.NewGuid(),
                TicketId = ticket.Id,
                Booking = ticket,
                OldDate = new DateTimeOffset(DateTime.SpecifyKind(ticket.BookingDate, DateTimeKind.Utc)),
                NewDate = newDateUtcOffset,
                Reason = dto.Reason,
                ReScheduledOn = DateTimeOffset.UtcNow
            };
            ticket.BookingDate = newBookingDate;

            _context.RescheduleTickets.Add(reschedule);

            await _context.SaveChangesAsync();

            return new ReScheduleResponseDto
            {
                Id = reschedule.Id,
                TicketId = reschedule.TicketId,
                OldDate = reschedule.OldDate,
                NewDate = reschedule.NewDate,
                Message = "Ticket rescheduled successfully"
            };
        }

    }
}



