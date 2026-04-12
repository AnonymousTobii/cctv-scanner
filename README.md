🔍 IoT Device Scanner - Advanced Edition
==========================================

A professional-grade cybersecurity IoT Device Scanner developed using C# and Guna UI, designed to identify devices connected to a local network with advanced features including MAC address detection, device type identification, ONVIF profile detection, and firmware version extraction.

**Developed by:** Anonymous Tobi  
**Version:** 2.0.0 Advanced  
**Category:** Cybersecurity / Network Awareness / IoT Security / Vulnerability Assessment  
**Platform:** Windows (.NET 8.0)

## 🛡️ Professional Project Description

The IoT Device Scanner Advanced Edition is an enterprise-ready desktop-based network reconnaissance tool created for cybersecurity education, ethical network analysis, and professional security assessments. It enables users to discover and enumerate IoT and network devices within a local subnet, helping identify potential attack surfaces, weak credentials, and improve network visibility.

### Key Advanced Features:

- **MAC Address Detection**: Automatically retrieves and displays MAC addresses for discovered devices
- **Vendor Identification**: Identifies device manufacturers using MAC OUI lookup (Hikvision, Dahua, Axis, Sony, etc.)
- **Device Type Classification**: Intelligently classifies devices as Cameras, NVRs, DVRs, or other network equipment
- **ONVIF Profile Detection**: Detects ONVIF-compatible devices for integration purposes
- **Firmware Version Extraction**: Extracts firmware version information from device responses
- **Enhanced Credential Testing**: Extended dictionary with manufacturer-specific default credentials
- **Modern Dark Theme UI**: Professional dark-themed interface optimized for security operations
- **Export Capabilities**: Export results to JSON or CSV formats for reporting and documentation
- **Multi-Port Scanning**: Scans common surveillance ports (80, 443, 554, 8000, 8080, 8081, 8443, 9000)
- **Parallel Processing**: High-performance concurrent scanning with configurable thread limits

### Suitable For:

- Cybersecurity professionals and students
- Ethical hacking labs and penetration testing
- Network administrators and IT security teams
- IoT security awareness and auditing
- Blue-team training and defensive security
- Physical security system assessments

## 📌 Key Capabilities

✅ Modern dark-themed responsive UI using Guna UI  
✅ Rapid local network scanning with parallel processing  
✅ Detection of IoT and non-IoT devices  
✅ MAC address retrieval via ARP table lookup  
✅ Vendor identification from MAC OUI database  
✅ Intelligent device type classification  
✅ ONVIF protocol detection  
✅ Firmware version extraction  
✅ Displays IP address, ports, and availability status  
✅ Comprehensive credential testing with 20+ default combinations  
✅ Export results to JSON and CSV formats  
✅ Real-time scan progress tracking  
✅ Beginner-friendly workflow  
✅ Optimized for cybersecurity learning environments  
✅ Minimal system resource usage  

## 🧠 Cybersecurity Use Cases

- Network reconnaissance (authorized environments only)
- IoT device inventory and visibility
- Identifying unknown or rogue devices on the network
- Learning subnet scanning fundamentals
- Ethical hacking and blue-team training
- Security audit and compliance assessments
- Default credential vulnerability assessment
- Physical security system hardening
- Network segmentation validation

## 🛠️ Technology Stack

- **Programming Language:** C#
- **Framework:** .NET 8.0 (Windows Forms)
- **UI Framework:** Guna.UI2.WinForms
- **Networking Libraries:** 
  - System.Net
  - System.Net.NetworkInformation
  - System.Net.Sockets
  - System.Net.Http
- **JSON Processing:** Newtonsoft.Json

## 🚀 Installation & Setup

### Requirements

- Windows 10/11 OS
- Visual Studio 2022 or newer
- .NET 8.0 SDK
- Guna UI library (included via NuGet)
- Administrator privileges (for ARP table access)

### Installation Steps

```bash
git clone https://github.com/AnonymousTobii/cctv-scanner
cd cctv-scanner
```

1. Open the solution in Visual Studio
2. Restore NuGet dependencies
3. Verify Guna UI references
4. Build and run the project

## 🔬 How the Scanner Works

1. **Subnet Identification**: Identifies the target local subnet based on user input
2. **Host Discovery**: Sends ICMP/ARP-based probes to detect active hosts
3. **Port Scanning**: Checks for open ports on discovered hosts
4. **Authentication Testing**: Tests common default credentials against HTTP/RTSP services
5. **MAC Address Lookup**: Retrieves MAC addresses from the ARP cache
6. **Vendor Identification**: Matches MAC OUI prefixes to known vendors
7. **Device Classification**: Analyzes response content to classify device types
8. **ONVIF Detection**: Probes for ONVIF service endpoints
9. **Firmware Extraction**: Parses response headers and content for version info
10. **Results Display**: Presents discovered devices in the modern UI grid

### Scan Results Include:

✔ Target IP address  
✔ MAC Address  
✔ Open ports  
✔ Device type (Camera, NVR, DVR, etc.)  
✔ Vendor/Manufacturer  
✔ Authentication status  
✔ Discovered credentials  
✔ ONVIF support status  
✔ Firmware version (if detectable)  

## ⚙️ Configuration Options

- **Max Parallel Threads**: Configure concurrent scanning threads (default: 50)
- **HTTP Request Timeout**: Adjust timeout for HTTP requests (default: 3 seconds)
- **Port Probe Timeout**: Configure TCP connection timeout (default: 500ms)
- **RTSP Connect Timeout**: Set RTSP connection timeout (default: 1000ms)
- **Certificate Validation**: Option to accept invalid SSL certificates

## 📊 Export Formats

### JSON Export
- Full structured data with all scan details
- Ideal for programmatic processing and integration
- Includes timestamps and diagnostic information

### CSV Export
- Spreadsheet-compatible format
- Easy to import into Excel or other tools
- Suitable for reporting and documentation

## ⚠️ Legal & Ethical Disclaimer

**This software is provided strictly for educational and authorized security testing purposes.**

- ✅ Scan only networks you own or have explicit written permission to test
- ✅ The author is not responsible for misuse or unauthorized use
- ⚠️ Unauthorized scanning may violate local, state, and federal laws
- ⚠️ Always obtain proper authorization before conducting network scans
- ⚠️ Respect privacy and confidentiality of network owners

**Responsible Use Guidelines:**
1. Only scan networks where you have ownership or authorization
2. Document all scanning activities for compliance purposes
3. Report discovered vulnerabilities responsibly
4. Do not use this tool for malicious purposes
5. Follow all applicable laws and regulations

## 🤝 Contributing

Contributions, improvements, and security enhancements are welcome!

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License.  
You are free to use, modify, and distribute it with attribution.

## 👤 Author

**Anonymous Tobi**  
Cybersecurity & Ethical Hacking Enthusiast  
Focused on ethical tools, learning platforms, and security awareness projects.

## 📞 Support

For questions, issues, or contributions, please open an issue on GitHub.

---

*Remember: With great power comes great responsibility. Use this tool ethically and legally.*
