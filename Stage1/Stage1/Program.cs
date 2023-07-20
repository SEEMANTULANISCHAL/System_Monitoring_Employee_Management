using System;
using System.Management;
using MongoDB.Driver;
using MongoDB.Bson;
using System.Collections.ObjectModel;


class Program
{
    static void Main(string[] args)
    {

        var mongoClient = new MongoClient("mongodb+srv://LOGS:mongo123@cluster0.9mkdcyx.mongodb.net/?authMechanism=SCRAM-SHA-1\r\n");
        var database = mongoClient.GetDatabase("LOGS");
        var collection = database.GetCollection<BsonDocument>("stage1");
        // Query for device specifications using WMI
        var computerSystemSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_ComputerSystem");
        var computerSystemCollection = computerSystemSearcher.Get();
        string osName = Environment.OSVersion.ToString();
        ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_BIOS");

        var serialNumber = string.Empty;
        var biosAttributes = new BsonDocument();
        foreach (ManagementObject obj in searcher.Get())
        {
            string biosManufacturer = obj["Manufacturer"]?.ToString();
            string biosVersion = obj["SMBIOSBIOSVersion"]?.ToString();
            string biosDate = obj["ReleaseDate"]?.ToString();
            string biosSerialNumber = obj["SerialNumber"]?.ToString();

            Console.WriteLine("BIOS Manufacturer: " + biosManufacturer);
            Console.WriteLine("BIOS Version: " + biosVersion);
            Console.WriteLine("BIOS Release Date: " + biosDate);
            Console.WriteLine("BIOS Serial Number: " + biosSerialNumber);
            Console.WriteLine();

            // Store the BIOS attributes in the biosAttributes document
            biosAttributes.Add("Manufacturer", biosManufacturer);
            biosAttributes.Add("Version", biosVersion);
            biosAttributes.Add("ReleaseDate", biosDate);
            biosAttributes.Add("SerialNumber", biosSerialNumber);
            serialNumber = biosSerialNumber;
        }


        var specfication = new BsonDocument();
        //sYSTEM SPEC Information section
        foreach (ManagementObject computerSystem in computerSystemCollection)
        {
            // Retrieve computer system properties
            string manufacturer = computerSystem["Manufacturer"].ToString();
            string model = computerSystem["Model"].ToString();
            ManagementObjectSearcher memorySearcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
            ulong totRamSize = 0;

            foreach (ManagementObject obj in memorySearcher.Get())
            {
                ulong ramSizeBytes = Convert.ToUInt64(obj["Capacity"]);
                totRamSize += ramSizeBytes;
            }
            double totRamSizes = totRamSize / (1024.0 * 1024.0 * 1024.0);


            string pcName = computerSystem["Name"].ToString();
            ManagementObjectSearcher deviceSearcher = new ManagementObjectSearcher("SELECT UUID FROM Win32_ComputerSystemProduct");
            var deviceCollection = deviceSearcher.Get();

            // Display computer system specifications
            Console.WriteLine("SYSTEM SPEIFICATION");
            Console.WriteLine("Manufacturer: " + manufacturer);
            Console.WriteLine("Model: " + model);
            Console.WriteLine("Total RAM Size: " + totRamSizes.ToString("F2") + " GB");
            Console.WriteLine("PC Name: " + pcName);

            specfication.Add("DeviceName", pcName);
            specfication.Add("Manufacturer", manufacturer);
            specfication.Add("Model", model);
            specfication.Add("Total Physical Memory", totRamSizes);






            var devid = string.Empty;
            foreach (ManagementObject device in deviceSearcher.Get())
            {
                string deviceId = device["UUID"].ToString();
                Console.WriteLine("Device ID: " + deviceId);

                devid = deviceId;
            }

            Console.WriteLine();




            //CPU SPEC
            var processorSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            var processorCollection = processorSearcher.Get();
            var CPUSpecification = new BsonArray();

            ManagementObjectSearcher cpuSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
            foreach (ManagementObject obj in cpuSearcher.Get())
            {
                var cpuInfo = new BsonDocument();
                string cpuManufacturer = obj["Manufacturer"].ToString();
                string cpuModel = obj["Name"].ToString();
                int cpuCores = int.Parse(obj["NumberOfCores"].ToString());
                uint cpuClockSpeed = uint.Parse(obj["MaxClockSpeed"].ToString());
                Console.WriteLine("CPU Manufacturer: " + cpuManufacturer);
                Console.WriteLine("CPU Model: " + cpuModel);
                Console.WriteLine("CPU Cores: " + cpuCores);
                Console.WriteLine("CPU Clock Speed: " + cpuClockSpeed + " MHz");
                Console.WriteLine();

                cpuInfo.Add("CPU Manufacturer", cpuManufacturer);
                cpuInfo.Add("CPU Model", cpuModel);
                cpuInfo.Add("CPU Cores", cpuCores);
                cpuInfo.Add("CPU Clock Speed (in MHz) ", cpuClockSpeed);

                CPUSpecification.Add(cpuInfo);
            }














            // Get RAM information
            ManagementObjectSearcher ramSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_PhysicalMemory");
            ulong totalRamSize = 0;
            var ramSpecification = new BsonArray();
            foreach (ManagementObject obj in ramSearcher.Get())
            {
                var ramInfo = new BsonDocument();
                string ramManufacturer = obj["Manufacturer"]?.ToString();
                string partNumber = obj["PartNumber"]?.ToString();
                ulong ramSizeBytes = Convert.ToUInt64(obj["Capacity"]);
                double ramSizeGB = ramSizeBytes / (1024.0 * 1024.0 * 1024.0);
                totalRamSize += ramSizeBytes;
                double totalRamSizeGB = totalRamSize / (1024.0 * 1024.0 * 1024.0);

                Console.WriteLine("RAM Manufacturer: " + ramManufacturer);
                Console.WriteLine("RAM Part Number: " + partNumber);
                Console.WriteLine("RAM Size: " + ramSizeGB.ToString("F2") + " GB");
                Console.WriteLine("Total RAM Size: " + totalRamSizeGB.ToString("F2") + " GB");

                ramInfo.Add("RAM Manufacturer", ramManufacturer);
                ramInfo.Add("RAM Part Number", partNumber);
                ramInfo.Add("RAM Size (in GB)", ramSizeGB);
                ramInfo.Add("Total RAM Size (in GB)", totalRamSizeGB);

                ramSpecification.Add(ramInfo);
            }



            Console.WriteLine();




            // Get Storage Device information
            ManagementObjectSearcher diskSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive");
            ulong totalDiskSizeBytes = 0;
            var stgspecfication = new BsonArray();
            foreach (ManagementObject obj in diskSearcher.Get())
            {
                var stgInfo = new BsonDocument();
                ulong diskSizeBytes = ulong.Parse(obj["Size"].ToString());
                totalDiskSizeBytes += diskSizeBytes;
                string diskModel = obj["Model"].ToString();
                double diskGB = diskSizeBytes / (1024.0 * 1024.0 * 1024.0);
                double totalDiskSizeGB = totalDiskSizeBytes / (1024.0 * 1024.0 * 1024.0);
                Console.WriteLine("Disk Model: " + diskModel);
                Console.WriteLine("Disk Size: " + diskGB.ToString("F2") + " GB");
                Console.WriteLine("Total Disk Size: " + totalDiskSizeGB.ToString("F2") + " GB");

                stgInfo.Add("Disk Model ", diskModel);
                stgInfo.Add("Disk Size (in GB)", diskGB);
                stgInfo.Add("Total Disk Size (in GB)", totalDiskSizeGB);
                stgspecfication.Add(stgInfo);

            }

            Console.WriteLine();


            // Get motherboard information
            ManagementObjectSearcher motherboardSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_BaseBoard");
            var mothspecfication = new BsonDocument();
            foreach (ManagementObject obj in motherboardSearcher.Get())
            {
                string motherboardManufacturer = obj["Manufacturer"]?.ToString();
                string motherboardModel = obj["Product"]?.ToString();
                string motherboardSerialNumber = obj["SerialNumber"]?.ToString();
                string motherboardVersion = obj["Version"]?.ToString();




                Console.WriteLine("Motherboard Manufacturer: " + motherboardManufacturer);
                Console.WriteLine("Motherboard Model: " + motherboardModel);
                Console.WriteLine("Motherboard Serial Number: " + motherboardSerialNumber);
                Console.WriteLine("Motherboard Version: " + motherboardVersion);
                Console.WriteLine();

                mothspecfication.Add("Motherboard Manufacturer ", motherboardManufacturer);
                mothspecfication.Add("Motherboard Model", motherboardModel);
                mothspecfication.Add("Motherboard Serial Number", motherboardSerialNumber);
                mothspecfication.Add("Motherboard Version", motherboardVersion);




            }

            // Check if GPU information is available
            ManagementObjectSearcher gpuSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");

            var gpuspecfication = new BsonArray();
            if (gpuSearcher.Get().Count > 0)
            {
                foreach (ManagementObject obj in gpuSearcher.Get())
                {
                    var gpuInfo = new BsonDocument();
                    string gpuManufacturer = obj["AdapterCompatibility"]?.ToString();
                    string gpuModel = obj["Name"]?.ToString();
                    ulong gpuMemory = obj["AdapterRAM"] != null ? ulong.Parse(obj["AdapterRAM"].ToString()) : 0;
                    double gpuGB = gpuMemory / (1024.0 * 1024.0 * 1024.0);

                    Console.WriteLine("GPU Manufacturer: " + gpuManufacturer);
                    Console.WriteLine("GPU Manufacturer: " + gpuModel);
                    Console.WriteLine("GPU Memory: " + gpuGB.ToString("F2") + " GB");
                    Console.WriteLine();

                    gpuInfo.Add("GPU Manufacturer", gpuManufacturer);
                    gpuInfo.Add("GPU Manufacturer ", gpuModel);
                    gpuInfo.Add("GPU Memory (in GB)", gpuGB);
                    gpuspecfication.Add(gpuInfo);


                }
            }
            else
            {
                Console.WriteLine("No GPU found.");
                Console.WriteLine();
            }


            // Get battery information
            ManagementObjectSearcher batterySearcher = new ManagementObjectSearcher("SELECT * FROM Win32_Battery");

            var batterySpecification = new BsonDocument();

            foreach (ManagementObject obj in batterySearcher.Get())
            {
                string batteryName = obj["Name"]?.ToString();
                int batteryStatus = Convert.ToInt32(obj["BatteryStatus"]);
                string batteryStatusString = GetBatteryStatus(batteryStatus);
                string batteryHealth = obj["Status"]?.ToString();
                string batteryVoltage = obj["DesignVoltage"]?.ToString();



                // Battery voltage in millivolts (mV)
                double voltageMV = double.Parse(batteryVoltage);
                // Convert voltage to volts (V)
                double voltageV = voltageMV / 1000.0;

                string batteryLevel = obj["EstimatedChargeRemaining"]?.ToString();
                string batteryRunTime = obj["EstimatedRunTime"]?.ToString();
                double estimatedCapacity = 0.0;
                if (!string.IsNullOrEmpty(batteryLevel) && !string.IsNullOrEmpty(batteryRunTime))
                {
                    int chargeRemaining = int.Parse(batteryLevel);
                    double runTimeInSeconds = double.Parse(batteryRunTime);
                    estimatedCapacity = (chargeRemaining / 100.0) * runTimeInSeconds / 3600.0; // Convert to hours
                }

                Console.WriteLine("Battery Name: " + batteryName);
                Console.WriteLine("Battery Status: " + batteryStatusString);

                // Map battery health status to a more accurate representation
                string batteryHealthString = GetBatteryHealth(batteryHealth);
                Console.WriteLine("Battery Health: " + batteryHealthString);

                Console.WriteLine("Battery Voltage: " + voltageV.ToString("F2") + " V");

                if (estimatedCapacity > 0.0)
                {
                    Console.WriteLine("Battery Capacity (Estimated): " + estimatedCapacity.ToString("F2") + " mWh");
                }
                else
                {
                    Console.WriteLine("Battery Capacity: Not available");
                }

                Console.WriteLine("Battery Level: " + batteryLevel + "%");

                batterySpecification.Add("Battery Name", batteryName);
                batterySpecification.Add("Battery Status", batteryStatusString);
                batterySpecification.Add("Battery Health", batteryHealthString);
                batterySpecification.Add("Battery Voltage", voltageV);
                batterySpecification.Add("Battery Level", batteryLevel);
            }

            // Function to get the battery status as a meaningful action
            string GetBatteryStatus(int statusCode)
            {
                switch (statusCode)
                {
                    case 1:
                        return "Discharging";
                    case 2:
                        return "Charging";
                    case 3:
                        return "Fully Charged";
                    case 4:
                        return "Low";
                    case 5:
                        return "Critical";
                    case 6:
                        return "Charging and High";
                    case 7:
                        return "Charging and Low";
                    case 8:
                        return "Charging and Critical";
                    case 9:
                        return "Undefined";
                    case 10:
                        return "Partially Charged";
                    default:
                        return "Unknown";
                }
            }
            // Function to get the battery health as a more accurate representation
            string GetBatteryHealth(string healthCode)
            {
                if (healthCode == "OK")
                {
                    return "Excellent";
                }
                else if (healthCode == "Not OK")
                {
                    return "Poor";
                }
                else
                {
                    return "Unknown";
                }
            }






            // Create a new document to store the operating system information
            var OSDOC = string.Empty;
            OSDOC = osName;


            Console.WriteLine("Operating System: " + osName);




            ManagementObjectSearcher displayAdapterSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_VideoController");

            var adapspecification = new BsonArray();
            foreach (ManagementObject obj in displayAdapterSearcher.Get())
            {
                var adpInfo = new BsonDocument();
                string adapterName = obj["Name"]?.ToString();
                string adapterDescription = obj["Description"]?.ToString();
                string adapterDriverVersion = obj["DriverVersion"]?.ToString();
                string adapterDriverDate = obj["DriverDate"]?.ToString();
                string adapterResolution = obj["CurrentHorizontalResolution"]?.ToString() + "x" + obj["CurrentVerticalResolution"]?.ToString();
                string adapterHardwareID = obj["PNPDeviceID"]?.ToString();

                Console.WriteLine("Adapter Name: " + adapterName);
                Console.WriteLine("Adapter Description: " + adapterDescription);
                Console.WriteLine("Adapter Driver Version: " + adapterDriverVersion);

                DateTime driverDateTime = ManagementDateTimeConverter.ToDateTime(adapterDriverDate);
                string formattedDriverDate = driverDateTime.ToString("yyyy-MM-dd");
                Console.WriteLine("Adapter Driver Date: " + formattedDriverDate);

                Console.WriteLine("Adapter Resolution: " + adapterResolution);
                Console.WriteLine("Adapter Hardware ID: " + adapterHardwareID);
                Console.WriteLine();

                adpInfo.Add("Adapter Name", adapterName);
                adpInfo.Add("Adapter Description ", adapterDescription);
                adpInfo.Add("Adapter Driver Version", adapterDriverVersion);
                adpInfo.Add("Adapter Driver Date", formattedDriverDate);
                adpInfo.Add("Adapter Resolution", adapterResolution);
                adpInfo.Add("Adapter Hardware ID", adapterHardwareID);
                adapspecification.Add(adpInfo);

            }
            var document = new BsonDocument
            {
                { "SerialNumber", serialNumber },
                { "OS_SPECIFICATION" ,OSDOC },
                { "DEVICE_ID", devid },
                { "BIOS", biosAttributes },
                { "DEVICE_SPECIFICATION", specfication },
                { "CPU_SPECIFICATION", CPUSpecification },
                { "RAM_SPECIFICATION",  ramSpecification },
                { "STORAGE_SPECIFICATION", stgspecfication },
                { "MOTHERBOARD_SPECIFICATION", mothspecfication },
                { "GPU_SPECIFICATION", gpuspecfication},
                { "BATTERY_SPECIFICATION", batterySpecification },

                { "ADAPTER_SPECIFICATION", adapspecification },

            };


            collection.InsertOne(document);
        }
        Console.ReadLine();// Hold the console screen
    }
}