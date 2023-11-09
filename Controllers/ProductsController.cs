using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Northwind.Data;
using Northwind.Models;

namespace Northwind.MySQL.Controllers;

[ApiController]
// เป็น Attribute ที่ระบุว่าคลาส ProductsController เป็นคลาสควบคุม (Controller) สำหรับ ASP.NET Core Web API และมีคุณสมบัติเพิ่มเติมที่เกี่ยวข้องกับการควบคุม API.
[Route("[controller]")]
//Route เป็น Attribute ที่ระบุว่า URL ของ API นี้จะใช้ชื่อคลาส ProductsController เป็นส่วนหนึ่งของ URL. นี่หมายถึง URL จะเริ่มต้นด้วย "/Products" โดยอัตโนมัติ
public class ProductsController : ControllerBase

//ประกาศคลาส ProductsController ที่สืบทอดจาก ControllerBase ซึ่งเป็นคลาสที่ให้คุณสมบัติพื้นฐานสำหรับควบคุม API ใน ASP.NET Core.
{
    private readonly NorthwindContext _context;

    // ประกาศตัวแปร _context ที่ใช้เพื่อเชื่อมต่อกับฐานข้อมูลและดำเนินการแก้ไขข้อมูลผลิตภัณฑ์.
    private readonly IMapper _mapper;
    //ประกาศตัวแปร _mapper ที่ใช้ในการแม็ปข้อมูลระหว่าง DTO (Data Transfer Object) และโมเดลของฐานข้อมูล.

    public ProductsController(NorthwindContext context, IMapper mapper)

    //เป็นคอนสตรักเตอร์ของ ProductsController ที่รับพารามิเตอร์ context และ mapper. พารามิเตอร์ context คือตัวแปรที่เชื่อมต่อกับฐานข้อมูล (ประมาณเช่น Entity Framework DbContext) และ mapper คือตัวแปรที่ใช้ในการแม็ปข้อมูล.
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
    {
        // ใช้ Entity Framework ในการดึงข้อมูลผลิตภัณฑ์จากฐานข้อมูล
        var productDtos = await _context.Products
            .Select(product => new ProductDto
            {
                ProductId = product.ProductId, // รหัสผลิตภัณฑ์
                ProductName = product.ProductName, // ชื่อผลิตภัณฑ์
                QuantityPerUnit = product.QuantityPerUnit, // ปริมาณต่อหน่วย
                UnitPrice = product.UnitPrice, // ราคาต่อหน่วย
                UnitsInStock = product.UnitsInStock, // จำนวนหน่วยคงคลัง
                UnitsOnOrder = product.UnitsOnOrder, // จำนวนหน่วยในคำสั่งซื้อ
                ReorderLevel = product.ReorderLevel, // ระดับการสั่งซื้อใหม่
                Discontinued = product.Discontinued, // สถานะ Discontinued (ยกเลิกการขาย)
                Category = new CategoryDto
                {
                    CategoryId = product.Category.CategoryId, // รหัสหมวดหมู่
                    CategoryName = product.Category.CategoryName // ชื่อหมวดหมู่
                },
                Supplier = new SupplierDto
                {
                    SupplierId = product.Supplier.SupplierId, // รหัสผู้จัดส่ง
                    CompanyName = product.Supplier.CompanyName // ชื่อบริษัทผู้จัดส่ง
                }
            })
            .ToListAsync();

        // ส่งข้อมูลผลิตภัณฑ์ในรูปแบบของ ProductDto กลับผู้ใช้
        return Ok(productDtos);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        // ใช้ Entity Framework ในการดึงข้อมูลผลิตภัณฑ์จากฐานข้อมูล
        var product = await _context.Products
            .Include(p => p.Category) // เชื่อมตาราง Category เพื่อดึงข้อมูลหมวดหมู่
            .Include(p => p.Orderdetails) // เชื่อมตาราง Orderdetails เพื่อดึงข้อมูลคำสั่งซื้อที่เกี่ยวข้อง
            .Include(p => p.Supplier) // เชื่อมตาราง Supplier เพื่อดึงข้อมูลผู้จัดส่ง
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null)
        {
            return NotFound(); // ส่ง HTTP 404 Not Found ถ้าไม่พบผลิตภัณฑ์
        }

        return product; // ส่งข้อมูลผลิตภัณฑ์กลับผู้ใช้
    }



    [HttpPut("{id}")]
    public async Task<IActionResult> EditProduct(int id, [FromBody] UpdateProductDto updatedProductDto)
    {
        // ตรวจสอบว่ารหัสผลิตภัณฑ์ในพารามิเตอร์ตรงกับรหัสผลิตภัณฑ์ในข้อมูลที่ส่งมา
        if (id != updatedProductDto.ProductId)
        {
            return BadRequest("The 'id' in the URL does not match the 'ProductId' in the request body.");
        }

        // ค้นหาผลิตภัณฑ์ที่ต้องการแก้ไข
        var existingProduct = await _context.Products.FindAsync(id);

        // ถ้าไม่พบผลิตภัณฑ์
        if (existingProduct == null)
        {
            return NotFound("Product with the specified ID was not found.");
        }

        // ใช้ AutoMapper เพื่ออัปเดตข้อมูลของผลิตภัณฑ์จากข้อมูลที่ส่งมา
        _mapper.Map(updatedProductDto, existingProduct);

        try
        {
            // บันทึกการเปลี่ยนแปลงลงในฐานข้อมูล
            await _context.SaveChangesAsync();
            return Ok(updatedProductDto);
        }
        catch (DbUpdateConcurrencyException)
        {
            // ตรวจสอบการขัดแย้งในการอัปเดตข้อมูล
            if (!ProductExists(id))
            {
                return NotFound("Product with the specified ID was not found.");
            }
            else
            {
                // กรณีอื่น ๆ ที่เกิดข้อผิดพลาด
                throw;
            }
        }
    }

    [HttpPost]
    public async Task<ActionResult<ProductDto>> CreateProduct(ProductDto productDto)
    {
        // ตรวจสอบว่าหมวดหมู่ (Category) ที่มี CategoryId ที่ระบุในฐานข้อมูลหรือไม่
        Category category = await _context.Categories.FindAsync(productDto.Category.CategoryId);

        // ตรวจสอบว่าผู้จัดส่ง (Supplier) ที่มี SupplierId ที่ระบุในฐานข้อมูลหรือไม่
        Supplier supplier = await _context.Suppliers.FindAsync(productDto.Supplier.SupplierId);

        if (category == null)
        {
            // หากหมวดหมู่ไม่มีอยู่ ให้สร้างหมวดหมู่ใหม่
            category = new Category
            {
                CategoryId = productDto.Category.CategoryId
            };

            // เพิ่มหมวดหมู่ใหม่ลงใน context และบันทึก
            _context.Categories.Add(category);
        }

        if (supplier == null)
        {
            // หากผู้จัดส่งไม่มีอยู่ ให้สร้างผู้จัดส่งใหม่
            supplier = new Supplier
            {
                SupplierId = productDto.Supplier.SupplierId
            };

            // เพิ่มผู้จัดส่งใหม่ลงใน context และบันทึก
            _context.Suppliers.Add(supplier);
        }

        // สร้างผลิตภัณฑ์ใหม่โดยใช้หมวดหมู่และผู้จัดส่ง (ที่มีหรือสร้างใหม่)
        Product product = new Product
        {
            // แม็ปคุณสมบัติอื่น ๆ จาก DTO
            ProductName = productDto.ProductName,
            // แม็ปคุณสมบัติอื่น ๆ ตามที่จำเป็น
            Category = category,
            Supplier = supplier
        };

        // เพิ่มผลิตภัณฑ์ใหม่ลงใน context และบันทึก
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        // แม็ป Entity ผลิตภัณฑ์ที่สร้างใหม่กลับเป็น DTO และส่งกลับ
        var createdProductDto = _mapper.Map<ProductDto>(product);

        return CreatedAtAction("GetProduct", new { id = createdProductDto.ProductId }, createdProductDto);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        // ค้นหาผลิตภัณฑ์โดยใช้รหัสผลิตภัณฑ์ (id)
        var product = await _context.Products.FindAsync(id);

        // ถ้าไม่พบผลิตภัณฑ์
        if (product == null)
        {
            return NotFound("Product with the specified ID was not found.");
        }

        // ค้นหาและลบรายละเอียดคำสั่งซื้อที่เกี่ยวข้อง
        var orderDetails = await _context.Orderdetails
            .Where(od => od.ProductId == id)
            .ToListAsync();

        _context.Orderdetails.RemoveRange(orderDetails);

        // ลบผลิตภัณฑ์
        _context.Products.Remove(product);

        try
        {
            // บันทึกการเปลี่ยนแปลงลงในฐานข้อมูล
            await _context.SaveChangesAsync();
            return NoContent();
        }
        catch (DbUpdateConcurrencyException)
        {
            // ตรวจสอบการขัดแย้งในการอัปเดตข้อมูล
            if (!ProductExists(id))
            {
                return NotFound("Product with the specified ID was not found.");
            }
            else
            {
                // กรณีอื่น ๆ ที่เกิดข้อผิดพลาด
                throw;
            }
        }
    }


    private bool ProductExists(int id)
    // เป็นการประกาศเมธอดชื่อ ProductExists ที่รับพารามิเตอร์เป็นรหัสผลิตภัณฑ์ (ID) และส่งค่าความจริง (true หรือ false) เป็นผลลัพธ์.
    {
        return _context.Products.Any(e => e.ProductId == id);
        //ใช้ LINQ (Language-Integrated Query) ในการตรวจสอบว่ามีผลิตภัณฑ์ในฐานข้อมูลที่มีรหัสผลิตภัณฑ์ (ProductID) ตรงกับรหัสที่ระบุ (id) หรือไม่. .Any() เป็นเมธอด LINQ ที่คืนค่า true ถ้ามีข้อมูลที่ตรงเงื่อนไข และ false ถ้าไม่มีข้อมูลที่ตรงเงื่อนไข.
    }
    //เมธอดช่วยในการตรวจสอบว่ามีผลิตภัณฑ์ที่มีรหัส (ID) ที่ระบุหรือไม่:

}