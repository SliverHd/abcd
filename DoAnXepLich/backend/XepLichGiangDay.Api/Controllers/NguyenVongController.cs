using Microsoft.AspNetCore.Mvc;
using XepLichGiangDay.Api.DTOs.NguyenVong;
using XepLichGiangDay.Api.Services;

namespace XepLichGiangDay.Api.Controllers;

[ApiController]
[Route("api/nguyen-vong")]
public class NguyenVongController(NguyenVongService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<NguyenVongDto>>> GetNguyenVong([FromQuery] int maHocKy, [FromQuery] int? maGiangVien)
    {
        var result = await service.LayNguyenVongAsync(maHocKy, maGiangVien);
        return Ok(result);
    }

    [HttpGet("lich-ban")]
    public async Task<ActionResult<List<LichBanDto>>> GetLichBan([FromQuery] int maHocKy, [FromQuery] int? maGiangVien)
    {
        var result = await service.LayLichBanAsync(maHocKy, maGiangVien);
        return Ok(result);
    }

    [HttpPost("luu-ma-tran")]
    public async Task<IActionResult> LuuMaTran([FromBody] LuuMaTranNguyenVongRequest request)
    {
        if (request.MaGiangVien <= 0 || request.MaHocKy <= 0)
        {
            return BadRequest("Mã giảng viên hoặc mã học kỳ không hợp lệ.");
        }

        var success = await service.LuuMaTranAsync(request);
        return success ? Ok(new { Message = "Lưu lịch bận và nguyện vọng thành công." }) : BadRequest("Không thể lưu thông tin.");
    }
}
