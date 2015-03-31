PROJECT := CCE

DIR_SOURCES := ./Source
DIR_PROPERTIES := ./Properties

SOURCES := $(shell find $(DIR_SOURCES) -name '*.cs')
SOURCES += $(shell find $(DIR_PROPERTIES) -name '*.cs')

DIR_BINARY := Binaries
BINARY_NAME := $(PROJECT).exe
BINARY_PATH := $(DIR_BINARY)/Debug/$(BINARY_NAME)

DEPENDENCY_PREFIX := Dependencies
DEPENDENCIES += Mono.Cecil.dll
DEPENDENCY_LIST := $(addprefix -r:, $(DEPENDENCIES))

$(BINARY_PATH): $(SOURCES)
	mkdir -p $(DIR_BINARY)/Debug
	mcs /target:exe -debug /out:"$(BINARY_PATH)" /lib:$(DEPENDENCY_PREFIX) $(DEPENDENCY_LIST) $(SOURCES)

clean:
	rm -r $(DIR_BINARY)