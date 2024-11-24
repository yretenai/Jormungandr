CLASS_HAS_NAME = True

rtti_logger = None

def read_pointer(reader):
	global bv
	if bv.arch.address_size == 1:
		return reader.read8() or 0
	if bv.arch.address_size == 2:
		return reader.read16() or 0
	if bv.arch.address_size == 4:
		return reader.read32() or 0
	if bv.arch.address_size == 8:
		return reader.read64() or 0
	return reader.read(bv.arch.address_size)


def is_valid_pointer(address):
	global bv
	if address == 0: # nullptr is valid
		return True
	if address < bv.image_base:
		return False
	for section_name in bv.sections:
		section = bv.sections[section_name]
		if address in section:
			return True
	return False


def address_to_section(address):
	global bv
	if address == 0:
		return 'nullptr'
	if address < bv.image_base:
		return "INVALID POINTER 0x%x" % (address)
	for section_name in bv.sections:
		section = bv.sections[section_name]
		if address in section:
			return '%s+0x%x' % (section.name, address - section.start)
	return 'base+0x%x' % (address - bv.image_base)


def is_call(op):
	if op == HighLevelILOperation.HLIL_CALL:
		return True
	if op == HighLevelILOperation.HLIL_TAILCALL:
		return True
	return False


class TypeInfo:
	bits: int
	is_primitive: bool
	is_bit_field: bool
	type_hash: int
	array_size: int
	type_id: int
	bit_size: int
	bit_field_size: int


	def __init__(self, bits: int):
		self.bits = bits

		self.type_hash = bits & 0xffffffff
		self.array_size = (bits >> 32) & 0x7fff
		self.is_primitive = ((bits >> 47) & 1) == 1
		self.type_id = (bits >> 48) & 0x3f
		self.is_bit_field = ((bits >> 54) & 1) == 1
		self.bit_size = (bits >> 55) & 0x3f
		self.bit_field_size = (bits >> 61) & 3


class AccessInfo:
	bits: int
	is_bit_field: bool
	is_dynamic: bool
	type_id: int
	bit_offset: int
	bit_size: int
	offset: int


	def __init__(self, bits: int):
		self.bits = bits

		self.type_id = bits & 0x1f
		self.bit_offset = (bits >> 5) & 0x3f
		self.bit_size = (bits >> 11) & 0x1f
		self.is_bit_field = ((bits >> 16) & 1) == 1
		self.is_dynamic = ((bits >> 17) & 1) == 1
		self.offset = (bits >> 18) & 0xffffffff


class MethodArgumentRTTI:
	address: str

	type_info: TypeInfo
	name_hash: int
	name: str
	direction: int


	def __init__(self, address: int):
		global bv
		self.address = address_to_section(address)

		reader = bv.reader(address)

		self.type_info = TypeInfo(reader.read64() or 0)
		self.name_hash = reader.read32() or 0

		val = bv.get_ascii_string_at(read_pointer(reader), 0)
		if val is not None:
			self.name = val.value
		else:
			self.name = ""

		self.direction = reader.read32()
		reader.read32() # padding


class MethodRTTI:
	address: str

	name: str
	name_hash: int
	arguments: [MethodArgumentRTTI]
	flags: int


	def __init__(self, address: int):
		global bv
		self.address = address_to_section(address)

		reader = bv.reader(address)

		val = bv.get_ascii_string_at(read_pointer(reader), 0)
		if val is not None:
			self.name = val.value
		else:
			self.name = ""

		arg_address = read_pointer(reader)
		arg_count = reader.read32() or 0
		self.name_hash = reader.read32() or 0
		self.flags = reader.read32() or 0
		reader.read32() # padding

		if arg_count > 0x7fff:
			return

		self.arguments = [None] * arg_count
		arg_size = 0x18 + bv.arch.address_size
		for index in range(arg_count):
			self.arguments[index] = MethodArgumentRTTI(arg_address)
			arg_address += arg_size



class EnumValueRTTI:
	value: int
	name_hash: int


	def __init__(self, address: int):
		global bv
		reader = bv.reader(address)
		self.value = reader.read32()
		self.name_hash = reader.read32()


class EnumRTTI:
	address: str

	name_hash: int
	values: [EnumValueRTTI]


	def __init__(self, address: int):
		global bv
		self.address = address_to_section(address)

		reader = bv.reader(address)
		values_address = read_pointer(reader)

		if not is_valid_pointer(values_address):
			return

		self.name_hash = reader.read32()

		value_count = reader.read16()

		if value_count > 0x7fff:
			return

		self.values = [None] * value_count
		value_size = 0x8
		for index in range(value_count):
			self.values[index] = EnumValueRTTI(values_address)
			values_address += value_size


class FieldRTTI:
	address: str

	flags: int
	name_hash: int
	type_info: int
	access_info: int

	address_1: str
	address_2: str
	address_3: str
	address_4: str


	def __init__(self, address: int):
		global bv
		self.address = address_to_section(address)

		reader = bv.reader(address)
		self.flags = reader.read32() or 0
		self.name_hash = reader.read32() or 0
		self.type_info = TypeInfo(reader.read64() or 0)
		self.access_info = AccessInfo(reader.read64() or 0)

		self.address_1 = address_to_section(read_pointer(reader))
		self.address_2 = address_to_section(read_pointer(reader))
		self.address_3 = address_to_section(read_pointer(reader))
		self.address_4 = address_to_section(read_pointer(reader))


class ClassRTTI:
	address: str

	fields: [FieldRTTI]
	enums: [EnumRTTI]
	methods: [MethodRTTI]
	parent_hash: int
	class_hash: int
	name: str
	size: int
	dynamic_properties_offset: int
	flags: int
	field_flags: int
	alignment: int
	parent_class: int
	signature: int
	index: int
	inheritance_min: int
	inheritance_max: int
	field_44_legacy: int
	field_4c_legacy: int

	constructor_address: str
	address_2: str
	address_3: str
	address_4: str
	address_5: str
	address_6: str
	address_7: str
	address_8: str
	deconstructor_address: str
	address_10: str


	def __init__(self, address: int, blob):
		global bv
		self.address = address_to_section(address)

		reader = bv.reader(address)
		fields_address = read_pointer(reader)
		enums_address = read_pointer(reader)
		methods_address = read_pointer(reader)

		if not is_valid_pointer(fields_address) or not is_valid_pointer(enums_address) or not is_valid_pointer(methods_address):
			return

		if CLASS_HAS_NAME:
			val = bv.get_ascii_string_at(read_pointer(reader), 0)
			if val is not None:
				self.name = val.value
			else:
				self.name = ""

		self.parent_hash = reader.read32() or 0
		self.class_hash = reader.read32() or 0
		self.size = reader.read32() or 0
		self.dynamic_properties_offset = reader.read32() or 0
		self.flags = reader.read64() or 0
		self.parent_class = read_pointer(reader)
		self.signature = reader.read64() or 0
		self.index = reader.read32() or 0
		if CLASS_HAS_NAME:
			self.field_44_legacy = reader.read32() or 0
			self.field_4c_legacy = reader.read32() or 0
		self.inheritance_min = reader.read16() or 0
		self.inheritance_max = reader.read16() or 0

		self.constructor_address = address_to_section(read_pointer(reader))
		self.address_2 = address_to_section(read_pointer(reader))
		self.address_3 = address_to_section(read_pointer(reader))
		self.address_4 = address_to_section(read_pointer(reader))
		self.address_5 = address_to_section(read_pointer(reader))
		self.address_6 = address_to_section(read_pointer(reader))
		self.address_7 = address_to_section(read_pointer(reader))
		self.address_8 = address_to_section(read_pointer(reader))
		self.deconstructor_address = address_to_section(read_pointer(reader))
		self.address_10 = address_to_section(read_pointer(reader))

		field_count = reader.read16() or 0
		enum_count = reader.read16() or 0
		method_count = reader.read16() or 0
		self.field_flags = reader.read16() or 0
		self.alignment = self.field_flags & 0x7f

		if field_count > 0x7fff or enum_count > 0x7fff or method_count > 0x7fff:
			return

		self.fields = [None] * field_count
		field_size = 0x18 + (bv.arch.address_size * 4)
		for index in range(field_count):
			self.fields[index] = FieldRTTI(fields_address)
			fields_address += field_size

		self.enums = [None] * enum_count
		enum_size = 0x6 + bv.arch.address_size
		for index in range(enum_count):
			self.enums[index] = EnumRTTI(enums_address)
			blob.enums[self.enums[index].name_hash] = self.enums[index]
			enums_address += enum_size

		self.methods = [None] * method_count
		method_size = 0x10 + (bv.arch.address_size * 2)
		for index in range(method_count):
			self.methods[index] = MethodRTTI(methods_address)
			methods_address += method_size

visited = set()

class RTTIBlob():
	classes: dict[int, ClassRTTI]
	enums: dict[int, EnumRTTI]
	names: [str]
	build: dict[str, str]


	def __init__(self):
		self.classes = {}
		self.enums = {}
		self.names = []
		self.build = {}


	def add_class(self, address: int):
		if address == 0 or not is_valid_pointer(address):
			return
		if address in visited:
			return
		visited.add(address)
		print('loading class from address 0x%x' % (address))
		rtti = ClassRTTI(address, self)
		if rtti.class_hash == 0:
			return
		self.classes[rtti.class_hash] = rtti


	def add_enum(self, address: int):
		if address == 0 or not is_valid_pointer(address):
			return
		if address in visited:
			return
		visited.add(address)
		print('loading enum from address 0x%x' % (address))
		rtti = EnumRTTI(address)
		if rtti.name_hash == 0:
			return
		self.enums[rtti.name_hash] = rtti


	def add_name(self, address: int):
		global bv
		if address == 0 or not is_valid_pointer(address):
			return
		if address in visited:
			return
		visited.add(address)
		print('loading name from address 0x%x' % (address))
		val = bv.get_ascii_string_at(address, 0)
		if val is not None:
			self.names.append(val.value)


	def add_build_id(self, address: int):
		global bv
		if address == 0 or not is_valid_pointer(address):
			return
		print('loading build_id from address 0x%x' % (address))
		val = bv.get_ascii_string_at(address, 0)
		if val is not None:
			text = val.value.strip().split(';')
			for entry in text:
				pair = entry.split(':', 1)
				if len(pair) > 1:
					self.build[pair[0]] = pair[1]


	def get_address_from_assign(self, hlil, index):
		global bv
		if hlil is None: return 0
		if len(hlil.instruction_operands) < index + 1: return 0
		op = hlil.instruction_operands[index]
		deref = False
		if op.operation == HighLevelILOperation.HLIL_DEREF:
			deref = True
			op = op.src
		if op.operation != HighLevelILOperation.HLIL_CONST_PTR: return 0
		dest = op.constant
		if deref and dest != 0:
			dest = bv.read_pointer(dest)
		return dest


	def get_address_from_call(self, hlil, index):
		if hlil is None: return 0
		if not is_call(hlil.operation):
			hlil = hlil.src
		return self.get_address_from_assign(hlil, index)


	def find_names(self, functions):
		for func in functions:
			for site in func.caller_sites:
				dest = self.get_address_from_call(site.hlil, 2)
				if dest != 0:
					self.add_name(dest)


	def find_classes_from_ctor(self, functions):
		for func in functions:
			for site in func.caller_sites:
				dest = self.get_address_from_call(site.hlil, 1)
				if dest != 0:
					self.add_class(dest)


	def add_class_from_registration(self, functions):
		for func in functions:
			for site in func.caller_sites:
				dest = self.get_address_from_call(site.hlil, 2)
				if dest != 0:
					self.add_class(dest)


	def add_enum_from_registration(self, functions):
		for func in functions:
			for site in func.caller_sites:
				dest = self.get_address_from_call(site.hlil, 2)
				if dest != 0:
					self.add_enum(dest)


	def load_registrations(self, functions):
		global bv
		for func in functions:
			for site in func.caller_sites:
				dest = self.get_address_from_call(site.hlil, 2)
				if dest == 0: continue
				self.load_registration(bv.get_function_at(dest))


	def load_stack(self, functions):
		for func in functions:
			for site in func.callers:
				for callee in site.callees:
					if callee.name == "TagAddClass" or callee.name == "TagAddEnum":
						self.load_registration(callee)
						break


	def load_registration(self, reg):
		global bv, rtti_logger
		print('loading rtti from address %s' % (reg.address_ranges))
		addresses = []
		for hlil in reg.hlil.instructions:
			if hlil.operation == HighLevelILOperation.HLIL_VAR_INIT or hlil.operation == HighLevelILOperation.HLIL_ASSIGN:
				var_addr = self.get_address_from_assign(hlil, 1)
				if var_addr == 0:
					var_addr = self.get_address_from_assign(hlil, 0)
				addresses.append(var_addr)
				continue
			if not is_call(hlil.operation): continue
			call_addr = self.get_address_from_call(hlil, 0)
			if call_addr == 0: continue
			call_method = bv.get_function_at(call_addr)
			if call_method is None:
				rtti_logger.log_error("invalid call at rtti %s (0x%x)" % (reg.address_ranges, call_addr))
				continue
			call_name = call_method.name
			if call_name == 'TagAddClass':
				for address in addresses:
					self.add_class(address)
				addresses = []
			elif call_name == 'TagAddEnum':
				for address in addresses:
					self.add_enum(address)
				break


	def load_build_id(self, data_vars):
		if len(data_vars) > 0:
			self.add_build_id(data_vars[0].address)

if __name__ == '__main__':
	from binaryninja import * # this will crash if you just try to run it in python, but executes fine in BinaryNinja

	rtti_logger = bv.create_logger("AnvilRTTI")

	rtti_logger.log_info("Begin Scan")

	rtti_blob = RTTIBlob()

	rtti_logger.log_info("Processing Names")
	rtti_blob.find_names(bv.get_functions_by_name('TagSetHeader'))

	rtti_logger.log_info("Processing RTTI Registration")
	rtti_blob.load_registrations(bv.get_functions_by_name('TagRegisterRTTI'))

	rtti_logger.log_info("Processing RTTI Stack")
	rtti_blob.load_stack(bv.get_functions_by_name("__chkstk"))

	rtti_logger.log_info("Processing Class Constructors")
	rtti_blob.find_classes_from_ctor(bv.get_functions_by_name('TagCreateClass'))

	rtti_logger.log_info("Processing Class Registration")
	rtti_blob.add_class_from_registration(bv.get_functions_by_name('TagAddClass'))

	rtti_logger.log_info("Processing Enum Registration")
	rtti_blob.add_enum_from_registration(bv.get_functions_by_name('TagAddEnum'))

	rtti_logger.log_info("Processing Build Identifier")
	rtti_blob.load_build_id(bv.get_symbols_by_name("BuildId"))

	rtti_logger.log_info("Done")

	name = rtti_blob.build['Exec'] if 'Exec' in rtti_blob.build else 'Anvil'
	if name.lower().endswith('.exe'):
		name = name[:-4]
	name += '.json'
	with open(name, 'w') as file:
		json.dump(rtti_blob, file, default=vars)
		file.write('\n')
	rtti_logger.log_info("Wrote %s" % (name))
