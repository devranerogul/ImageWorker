import './style.css'
// import fabric
import { FabricText, Canvas, FabricImage, Textbox, Shadow } from 'fabric';

// import config
const config = {
  apiUrl: 'https://localhost:7017',
  endpoints: {
    createTemplate: '/template/create',
    generateImage: '/image/generate'
  },
  clientId: 'kevlar-ui',
  defaultLanguage: 'en-US',
  defaultCanvasWidth: 600,
  defaultCanvasHeight: 600,
};

// Add fonts array at the top of the file
const fonts = [
  { name: 'Inter', value: 'Inter' },
  { name: 'Roboto', value: 'Roboto' },
  { name: 'Open Sans', value: 'Open Sans' },
  { name: 'Lato', value: 'Lato' },
  { name: 'Poppins', value: 'Poppins' },
  { name: 'Montserrat', value: 'Montserrat' },
  { name: 'Raleway', value: 'Raleway' },
  { name: 'Ubuntu', value: 'Ubuntu' },
  { name: 'Playfair Display', value: 'Playfair Display' },
  { name: 'Source Sans Pro', value: 'Source Sans Pro' }
];

// Add counters at the top of the file
let textLayerCounter = 1;
let imageLayerCounter = 1;

// Initialize canvas with selection and control styles
let canvas = new Canvas('canvas', {
  controlsAboveOverlay: true,
  backgroundColor: '#ffffff',
  width: window.innerWidth - 450,
  height: window.innerHeight - 140,
  selectionBorderColor: '#2196F3',
  selectionLineWidth: 2,
  transparentCorners: false,
  cornerColor: '#2196F3',
  cornerStrokeColor: '#fff',
  cornerSize: 10,
  padding: 5
});

canvas.on('selection:created', (e) => setSelectedObject(e.selected[0]));
canvas.on('selection:updated', (e) => setSelectedObject(e.selected[0]));
canvas.on('selection:cleared', () => setSelectedObject(null));

canvas.renderAll();

// Template name functionality
function initializeTemplateName() {
  const templateName = document.querySelector('.template-name');
  if (!templateName) return;

  // Set initial name from localStorage or default to "Untitled"
  const savedName = localStorage.getItem('templateName');
  templateName.textContent = savedName || 'Untitled';

  templateName.addEventListener('click', function() {
    if (!this.classList.contains('editing')) {
      this.classList.add('editing');
      this.contentEditable = true;
      this.focus();
    }
  });

  templateName.addEventListener('keydown', function(e) {
    if (e.key === 'Enter') {
      e.preventDefault();
      this.blur();
    }
  });

  templateName.addEventListener('blur', function() {
    this.classList.remove('editing');
    this.contentEditable = false;
    const newName = this.textContent.trim();
    if (newName) {
      localStorage.setItem('templateName', newName);
    } else {
      this.textContent = 'Untitled';
      localStorage.removeItem('templateName');
    }
  });
}

// Add to your initialization code
document.addEventListener('DOMContentLoaded', async () => {
  const params = getUrlParams();
  if (params.id) {
    await loadTemplate(params.id);
  }else{
    // Clear localstorage
    localStorage.removeItem('templateId');
    localStorage.removeItem('templateName');
  }
  initializeTemplateName();
});

// add text to canvas when button is clicked
document.querySelector('.add-text').addEventListener('click', () => {
  const text = new Textbox('New Text', {
    left: 50 + Math.random() * 100,
    top: 100 + Math.random() * 100,
    width: 200,
    selectable: true,
    editable: true,
    fontFamily: 'Inter',
    fontSize: 20,
    fill: '#000000',
    id: Math.random().toString(36).substring(2, 7) + (Math.random() * 100000).toFixed(0),
    name: `Text Layer ${textLayerCounter++}`,
    shadow: null // Start with no shadow
  });
  canvas.add(text);
  canvas.renderAll();
  canvas.setActiveObject(text);
});


// when clicked to new button, clear the canvas, clear the localstorage and clear the properties panel
document.querySelector('.new-button').addEventListener('click', () => {
  canvas.clear();

  localStorage.removeItem('templateId');
  localStorage.removeItem('templateName');  // Add this line
  
  // Reset template name to Untitled
  const templateName = document.querySelector('.template-name');
  if (templateName) {
    templateName.textContent = 'Untitled';
  }
  
  // Clear properties panel 
  clearPropertiesPanel();

  // Clear existing canvas
  //canvas.dispose();
  
  // Create new canvas
  //canvas = new Canvas('canvas', {
  //  controlsAboveOverlay: true,
  //  backgroundColor: '#ffffff',
  //  width: window.innerWidth - 450,
  //  height: window.innerHeight - 140,
  //});

  // Reattach canvas event listeners
  //canvas.on('selection:created', (e) => setSelectedObject(e.selected[0]));
  //canvas.on('selection:updated', (e) => setSelectedObject(e.selected[0]));
  //canvas.on('selection:cleared', () => setSelectedObject(null));
  
  // Render the new canvas
  //canvas.renderAll();
});

// when save button is clicked, save the canvas as a JSON file 
// and call API template/generate to save the template 
// so request body should be created from canvas to comply with the API request body which is a TemplateCreationRequest
document.querySelector('.save-button').addEventListener('click', async () => {

  const saveButton = document.querySelector('.save-button');
  const saveProgress = document.querySelector('.save-progress');
  const saveStatus = document.querySelector('.save-status');

  // Disable button and show progress
  saveButton.disabled = true;
  saveProgress.style.display = 'inline-flex';

  try {

    // if cavas is empty, show error message
    if (canvas.getObjects().length === 0) {
      saveStatus.style.color = 'red';
      saveStatus.textContent = 'Error: Canvas is empty';
      setTimeout(() => {
        saveProgress.style.display = 'none';
        saveStatus.textContent = 'Saving template...';
      }, 3000);
      return;
    }

    // if localstorage has templateId then update the template else create a new template
    const templateId = localStorage.getItem('templateId');

    await updateOrCreateTemplate(templateId);

    saveStatus.style.color = 'cyan';
    saveStatus.textContent = 'Template saved successfully!';

    setTimeout(() => {
      saveProgress.style.display = 'none';
      saveStatus.textContent = 'Saving template...';
    }, 2000);

  } catch (error) {
    console.error('Error saving template:', error);
    saveStatus.textContent = `Error: ${error.message}`;
    setTimeout(() => {
      saveProgress.style.display = 'none';
      saveStatus.textContent = 'Saving template...';
    }, 3000);
  } finally {
    // Re-enable the save button
    saveButton.disabled = false;
  }

});

const updateOrCreateTemplate = async (templateId) => {
  if (templateId) {
    await updateTemplate(templateId);
  } else {
    await createTemplate();
  }
}

const updateTemplate = async (templateId) => {
  const templateUpdateRequest = getTemplateRequest();

  console.log('Updating template :', `${config.apiUrl}${config.endpoints.createTemplate}`);

  const response = await fetch(`${config.apiUrl}${config.endpoints.createTemplate}`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      templateId: templateId,
      ...templateUpdateRequest
    })
  });
  if (!response.ok) {
    throw new Error(`Failed to update template: ${response.status} ${response.statusText}`);
  }
  //const data = await response.json();
  
  return true;
}

const createTemplate = async () => {

  const templateCreationRequest = getTemplateRequest();

  console.log('Saving template to:', `${config.apiUrl}${config.endpoints.createTemplate}`);
  console.log('Request:', templateCreationRequest);

  const response = await fetch(`${config.apiUrl}${config.endpoints.createTemplate}`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json'
    },
    body: JSON.stringify(templateCreationRequest)
  });

  if (!response.ok) {
    throw new Error(`Failed to save template: ${response.status} ${response.statusText}`);
  }

  const data = await response.json();

  // Save the template ID to local storage
  localStorage.setItem('templateId', data.id);

  return true;
}

const getTemplateRequest = () => {
    
  const objects = canvas.getObjects();
    

  const imageLayers = objects.filter(obj => obj.type === 'image').map(obj => {
    return {
      id: obj.id,
      name: obj.name,
      imageData: {
        path: obj.getSrc() || obj._element?.src || ''
      },
      position: {
        x: Math.round(obj.left),
        y: Math.round(obj.top)
      },
      size: {
        width: Math.round(obj.getScaledWidth()),
        height: Math.round(obj.getScaledHeight())
      },
      opacity: obj.opacity || 1.0
    };
  });
  const textLayers = objects.filter(obj => obj.type === 'textbox').map(obj => {
    return {
      id: obj.id,
      name: obj.name,
      textData: {
        content: obj.text,
        font: {
          family: obj.fontFamily,
          size: Math.round(obj.fontSize),
          color: obj.fill,
          isBold: obj.fontWeight === 'bold',
          isItalic: obj.fontStyle === 'italic'
        }
      },
      position: {
        x: Math.round(obj.left),
        y: Math.round(obj.top)
      },
      size: {
        width: Math.round(obj.getScaledWidth()),
        height: Math.round(obj.getScaledHeight())
      },
      opacity: obj.opacity || 1.0
    };
  });

  const templateCreationRequest = {
    templateName: localStorage.getItem('templateName') || 'Untitled',
    description: 'Template created from Kevlar Editor',
    canvasWidth : canvas.getWidth(),
    canvasHeight : canvas.getHeight(),
    imageLayers,
    textLayers
  };

  return templateCreationRequest;
}

// add image to canvas when button is clicked
document.querySelector('.add-image').addEventListener('click', () => {
  // Open a prompt to enter the image URL
  const imageUrl = prompt('Enter the image URL:');

  // Load the image from the URL
  const image = new Image();
  image.src = imageUrl;
  image.onload = () => {
    const _cntr = imageLayerCounter++;
    const fabricImage = new FabricImage(image, { left: 50, top: 100, name: `Image Layer ${_cntr}` }); // Add layer name
    fabricImage.id = Math.random().toString(36).substring(2, 7) + (Math.random() * 100000).toFixed(0);
    fabricImage.name = "Image Layer " + _cntr;
    canvas.add(fabricImage);
    canvas.renderAll();
    canvas.setActiveObject(fabricImage);
  };
});

// when click on canvas, set the active object to the object under the cursor
canvas.on('mouse:down', (event) => {
  const object = canvas.getActiveObject();
  if (object) {
    canvas.setActiveObject(object);
    setSelectedObject(object);
  } else {
    // Clear properties panel when clicking empty canvas
    clearPropertiesPanel();
  }
});

// set the selected object and update properties panel
let selectedObject;
function setSelectedObject(object) {
  selectedObject = object;
  
  const propertiesPanel = document.querySelector('.properties-panel');

  // Create properties HTML
  let propertiesHTML = '';
  
  if (!object) {
    propertiesHTML = `
      <div class="properties-content">
        <p>No object selected</p>
      </div>
    `;
  } else {
    // Common properties for all objects
    propertiesHTML = `
      <div class="properties-content">
        <div class="property-group">
          <label>Layer Name</label>
          <input type="text" class="property-input" data-property="name" value="${object.name || ''}">
        </div>
        <div class="property-row">
          <div class="property-group">
            <label>Position X</label>
            <input type="number" class="property-input" data-property="left" value="${Math.round(object.left)}">
          </div>
          <div class="property-group">
            <label>Position Y</label>
            <input type="number" class="property-input" data-property="top" value="${Math.round(object.top)}">
          </div>
        </div>

        <div class="layer-controls">
          <h3>Layer Order</h3>
          <div class="layer-buttons">
            <button class="tool-button bring-front" title="Bring to Front">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <rect x="4" y="14" width="16" height="4" rx="1" fill="#666666"/>
                <rect x="6" y="10" width="12" height="4" rx="1" fill="#888888"/>
                <rect x="8" y="6" width="8" height="4" rx="1" fill="currentColor"/>
              </svg>
            </button>
            <button class="tool-button bring-forward" title="Bring Forward">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <rect x="4" y="14" width="16" height="4" rx="1" fill="#666666"/>
                <rect x="6" y="8" width="12" height="4" rx="1" fill="currentColor"/>
              </svg>
            </button>
            <button class="tool-button send-backward" title="Send Backward">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <rect x="6" y="12" width="12" height="4" rx="1" fill="currentColor"/>
                <rect x="4" y="6" width="16" height="4" rx="1" fill="#666666"/>
              </svg>
            </button>
            <button class="tool-button send-back" title="Send to Back">
              <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <rect x="8" y="14" width="8" height="4" rx="1" fill="currentColor"/>
                <rect x="6" y="10" width="12" height="4" rx="1" fill="#888888"/>
                <rect x="4" y="6" width="16" height="4" rx="1" fill="#666666"/>
              </svg>
            </button>
          </div>
        </div>
    `;

    if (object.type === 'textbox') {
      // Create font options HTML
      const fontOptionsHTML = fonts.map(font => 
        `<option value="${font.value}" ${object.fontFamily === font.value ? 'selected' : ''}>${font.name}</option>`
      ).join('');

      // Get shadow properties or set defaults
      const shadow = object.shadow || { 
        color: 'rgba(0,0,0,0)',
        blur: 0,
        offsetX: 0,
        offsetY: 0
      };

      const shadowColor = shadow.color || 'rgba(0,0,0,0)';
      const shadowOpacity = shadowColor.startsWith('rgba') ? 
        parseFloat(shadowColor.split(',')[3]) : 
        0;
      const shadowHexColor = shadowColor.startsWith('rgba') ? 
        '#000000' : 
        shadowColor;

      propertiesHTML += `
        <div class="property-group">
          <label>Font Family</label>
          <select class="property-input" data-property="fontFamily">
            ${fontOptionsHTML}
          </select>
        </div>
        <div class="property-group">
          <label>Text Color</label>
          <input type="color" class="property-input" data-property="fill" value="${object.fill || '#000000'}">
        </div>
        <div class="property-group">
          <label>Font Size</label>
          <input type="number" class="property-input" data-property="fontSize" value="${object.fontSize || 20}">
        </div>
        <div class="property-group shadow-controls">
          <label>Drop Shadow</label>
          <div class="shadow-properties">
            <div class="shadow-row">
              <label>Color</label>
              <input type="color" class="property-input shadow-color" value="${shadowHexColor}">
              <input type="range" class="property-input shadow-opacity" min="0" max="1" step="0.1" value="${shadowOpacity}">
            </div>
            <div class="shadow-row">
              <label>Blur</label>
              <input type="range" class="property-input shadow-blur" min="0" max="50" value="${shadow.blur || 0}">
            </div>
            <div class="shadow-row">
              <label>Offset X</label>
              <input type="range" class="property-input shadow-offset-x" min="-50" max="50" value="${shadow.offsetX || 0}">
            </div>
            <div class="shadow-row">
              <label>Offset Y</label>
              <input type="range" class="property-input shadow-offset-y" min="-50" max="50" value="${shadow.offsetY || 0}">
            </div>
          </div>
        </div>
      `;
    } else if (object.type === 'image') {
      propertiesHTML += `
        <div class="property-group">
          <label>Opacity</label>
          <input type="range" min="0" max="1" step="0.1" class="property-input" data-property="opacity" value="${object.opacity || 1}">
        </div>
      `;
    }

    propertiesHTML += `</div>`; // Close properties-content
  }

  // Generate the layer list HTML
  const layerListHTML = generateLayerList();

  // Add layer list at the bottom
  propertiesHTML += `
    <div class="layer-list">
      <h3>Layers</h3>
      ${layerListHTML}
    </div>
  `;

  // Set the final HTML
  propertiesPanel.innerHTML = propertiesHTML;

  // Add event listeners to inputs
  const inputs = propertiesPanel.querySelectorAll('.property-input');
  inputs.forEach(input => {
    input.addEventListener('change', (e) => {
      const property = e.target.dataset.property;
      const value = property === 'fontSize' ? parseInt(e.target.value) : 
                    property === 'opacity' ? parseFloat(e.target.value) : 
                    e.target.value;
      selectedObject.set(property, value);
      canvas.renderAll();
    });
  });

  // Add shadow control event listeners
  if (object.type === 'textbox') {
    const shadowColor = propertiesPanel.querySelector('.shadow-color');
    const shadowOpacity = propertiesPanel.querySelector('.shadow-opacity');
    const shadowBlur = propertiesPanel.querySelector('.shadow-blur');
    const shadowOffsetX = propertiesPanel.querySelector('.shadow-offset-x');
    const shadowOffsetY = propertiesPanel.querySelector('.shadow-offset-y');

    const updateShadow = () => {
      const color = shadowColor.value;
      const opacity = shadowOpacity.value;
      const r = parseInt(color.slice(1,3), 16);
      const g = parseInt(color.slice(3,5), 16);
      const b = parseInt(color.slice(5,7), 16);
      const rgba = `rgba(${r},${g},${b},${opacity})`;
      
      if (opacity === 0) {
        selectedObject.set('shadow', null);
      } else {
        selectedObject.set('shadow', new Shadow({
          color: rgba,
          blur: parseInt(shadowBlur.value),
          offsetX: parseInt(shadowOffsetX.value),
          offsetY: parseInt(shadowOffsetY.value)
        }));
      }
      canvas.renderAll();
    };

    [shadowColor, shadowOpacity, shadowBlur, shadowOffsetX, shadowOffsetY].forEach(input => {
      input.addEventListener('input', updateShadow);
    });
  }

  // Add event listeners for layer ordering buttons
  const bringToFront = propertiesPanel.querySelector('.bring-front');
  const bringForward = propertiesPanel.querySelector('.bring-forward');
  const sendBackward = propertiesPanel.querySelector('.send-backward');
  const sendToBack = propertiesPanel.querySelector('.send-back');

  bringToFront.addEventListener('click', () => {
    console.log(selectedObject);
    canvas.bringObjectToFront(selectedObject);
    canvas.renderAll();
  });

  bringForward.addEventListener('click', () => {
    canvas.bringObjectForward(selectedObject);
    canvas.renderAll();
  });

  sendBackward.addEventListener('click', () => {
    canvas.sendObjectBackwards(selectedObject);
    canvas.renderAll();
  });

  sendToBack.addEventListener('click', () => {
    canvas.sendObjectToBack(selectedObject);
    canvas.renderAll();
  });
}

// Add new function to generate layer list HTML
function generateLayerList() {
  const objects = canvas.getObjects();
  return objects.map((obj, index) => {
    const isSelected = obj === selectedObject;
    const layerType = obj.type === 'textbox' ? 'Text' : 'Image';
    return `
      <div class="layer-item ${isSelected ? 'selected' : ''}" data-index="${index}">
        <span class="layer-type">${layerType}</span>
        <span class="layer-name">${obj.name || `Layer ${index + 1}`}</span>
      </div>
    `;
  }).join('');
}

// Add click event listener for layer list
document.querySelector('.properties-panel').addEventListener('click', (e) => {
  const layerItem = e.target.closest('.layer-item');
  if (layerItem) {
    e.preventDefault();
    e.stopPropagation();
    
    const index = parseInt(layerItem.dataset.index);
    const objects = canvas.getObjects();
    const obj = objects[index];
    
    if (obj) {
      // Deselect current object if any
      canvas.discardActiveObject();
      
      // Set the new object as active
      canvas.setActiveObject(obj);
      
      // If it's a text object, make it editable
      if (obj.type === 'textbox') {
        obj.enterEditing();
        canvas.requestRenderAll();
      }
      
      // Update the properties panel
      setSelectedObject(obj);
      
      // Render canvas changes
      canvas.renderAll();
    }
  }
});

function clearPropertiesPanel() {
  const propertiesPanel = document.querySelector('.properties-panel');
  propertiesPanel.innerHTML = '<p>No object selected</p>';
}

// Zoom functionality
const zoomInButton = document.querySelector('.zoom-in');
const zoomOutButton = document.querySelector('.zoom-out');
const zoomLevelDisplay = document.querySelector('.zoom-level');
const canvasWrapper = document.querySelector('.canvas-wrapper');
let zoomLevel = 1;

// Update zoom display and wrapper
function updateZoomLevel() {
  zoomLevelDisplay.textContent = `${Math.round(zoomLevel * 100)}%`;
  canvasWrapper.style.transform = `scale(${zoomLevel})`;
  
  // Add padding to wrapper based on zoom level to ensure proper centering
  const padding = Math.max(20, 20 * zoomLevel);
  canvasWrapper.style.padding = `${padding}px`;
}

// Initial padding
canvasWrapper.style.padding = '20px';

// Zoom in
zoomInButton.addEventListener('click', () => {
  if (zoomLevel < 5) { // Max zoom 500%
    zoomLevel *= 1.12;
    canvas.setZoom(zoomLevel);
    updateZoomLevel();
  }
});

// Zoom out
zoomOutButton.addEventListener('click', () => {
  if (zoomLevel > 0.1) { // Min zoom 10%
    zoomLevel /= 1.12;
    canvas.setZoom(zoomLevel);
    updateZoomLevel();
  }
});

// Mouse wheel zoom
canvas.on('mouse:wheel', function(opt) {
  const delta = opt.e.deltaY;
  let zoom = canvas.getZoom();
  
  if (opt.e.ctrlKey || opt.e.metaKey) {
    opt.e.preventDefault();
    opt.e.stopPropagation();
    
    zoom *= 0.999 ** delta;
    if (zoom > 5) zoom = 5;
    if (zoom < 0.1) zoom = 0.1;
    
    // Get mouse position
    const pointer = canvas.getPointer(opt.e);
    
    // Set zoom point to mouse position
    canvas.zoomToPoint({ x: pointer.x, y: pointer.y }, zoom);
    
    zoomLevel = zoom;
    updateZoomLevel();
  }
});

// Center canvas initially
function centerCanvas() {
  const canvasContainer = document.querySelector('.canvas-container');
  const scrollX = (canvasContainer.scrollWidth - canvasContainer.clientWidth) / 2;
  const scrollY = (canvasContainer.scrollHeight - canvasContainer.clientHeight) / 2;
  canvasContainer.scrollTo(scrollX, scrollY);
}

// Center canvas after initialization
setTimeout(centerCanvas, 100);

// Pan functionality
let isPanning = false;
let lastPosX;
let lastPosY;

canvas.on('mouse:down', function(opt) {
  if (opt.e.altKey) {
    isPanning = true;
    canvas.selection = false;
    lastPosX = opt.e.clientX;
    lastPosY = opt.e.clientY;
  }
});

canvas.on('mouse:move', function(opt) {
  if (isPanning) {
    const deltaX = opt.e.clientX - lastPosX;
    const deltaY = opt.e.clientY - lastPosY;
    
    lastPosX = opt.e.clientX;
    lastPosY = opt.e.clientY;
    
    const canvasContainer = document.querySelector('.canvas-container');
    canvasContainer.scrollBy(-deltaX, -deltaY);
  }
});

canvas.on('mouse:up', function() {
  isPanning = false;
  canvas.selection = true;
});

// Canvas settings functionality
const settingsButton = document.querySelector('.settings-button');
const modal = document.getElementById('canvasSettingsModal');
const applyButton = document.getElementById('applyCanvasSize');
const cancelButton = document.getElementById('cancelCanvasSize');
const widthInput = document.getElementById('canvasWidth');
const heightInput = document.getElementById('canvasHeight');

// Show modal with current canvas dimensions
settingsButton.addEventListener('click', () => {
  widthInput.value = canvas.width;
  heightInput.value = canvas.height;
  modal.style.display = 'block';
});

// Apply new canvas dimensions
applyButton.addEventListener('click', () => {
  const newWidth = parseInt(widthInput.value);
  const newHeight = parseInt(heightInput.value);
  
  if (newWidth > 0 && newHeight > 0) {
    canvas.setWidth(newWidth);
    canvas.setHeight(newHeight);
    canvas.renderAll();
    modal.style.display = 'none';
  }
});

// Close modal on cancel
cancelButton.addEventListener('click', () => {
  modal.style.display = 'none';
});

// Close modal when clicking outside
window.addEventListener('click', (event) => {
  if (event.target === modal) {
    modal.style.display = 'none';
  }
});

// Add function to load template
async function loadTemplate(templateId) {
  try {
    // Fetch template data from API
    const response = await fetch(`${config.apiUrl}/template/${templateId}`);
    if (!response.ok) {
      throw new Error(`Failed to load template: ${response.statusText}`);
    }
    
    const template = await response.json();
    
    // Clear existing canvas and set white background
    canvas.clear();
    canvas.backgroundColor = '#ffffff';

    canvas.width = template.canvasWidth;
    canvas.height = template.canvasHeight;

    canvas.renderAll();    
    
    // Load text layers
    for (const textLayer of template.textLayers) {
      const text = new Textbox(textLayer.textData.content, {
        left: textLayer.position.x,
        top: textLayer.position.y,
        width: textLayer.size.width,
        height: textLayer.size.height,
        selectable: true,
        editable: true,
        fontFamily: textLayer.textData.font.family,
        fontSize: textLayer.textData.font.size,
        fill: textLayer.textData.font.color,
        id: textLayer.id,
        name: textLayer.name,
        opacity: textLayer.opacity,
        padding: 5,
        cornerSize: 10,
        transparentCorners: false,
        cornerColor: '#2196F3',
        cornerStrokeColor: '#fff',
        borderColor: '#2196F3',
        borderScaleFactor: 2
      });
      canvas.add(text);
    }
    
    // Load image layers
    for (const imageLayer of template.imageLayers) {
      // Create image element
      const img = new Image();
      img.crossOrigin = "anonymous";
      img.src = imageLayer.imageData.path;
      
      // Wait for image to load
      await new Promise((resolve, reject) => {
        img.onload = resolve;
        img.onerror = reject;
      });
      
      // Create fabric image object
      const fabricImage = new FabricImage(img, {
        left: imageLayer.position.x,
        top: imageLayer.position.y,
        width: imageLayer.size.width,
        height: imageLayer.size.height,
        selectable: true,
        id: imageLayer.id,
        name: imageLayer.name,
        opacity: imageLayer.opacity,
        padding: 5,
        cornerSize: 10,
        transparentCorners: false,
        cornerColor: '#2196F3',
        cornerStrokeColor: '#fff',
        borderColor: '#2196F3',
        borderScaleFactor: 2
      });
      
      canvas.add(fabricImage);
    }
    
    // Store template info in localStorage
    localStorage.setItem('templateId', template.templateId);
    localStorage.setItem('templateName', template.templateName);
    
    // Update template name in UI
    const templateName = document.querySelector('.template-name');
    if (templateName) {
      templateName.textContent = template.templateName;
    }
    
    // Render the canvas
    canvas.renderAll();
    
  } catch (error) {
    console.error('Error loading template:', error);
    // You might want to show an error message to the user
  }
}

// Add function to parse URL parameters
function getUrlParams() {
  const params = new URLSearchParams(window.location.search);
  return Object.fromEntries(params.entries());
}

document.addEventListener('click', (e) => {
  if (!selectedObject) return;

  if (e.target.closest('.bring-front')) {
    selectedObject.bringToFront();
    canvas.renderAll();
  } else if (e.target.closest('.bring-forward')) {
    selectedObject.bringForward();
    canvas.renderAll();
  } else if (e.target.closest('.send-backward')) {
    selectedObject.sendBackwards();
    canvas.renderAll();
  } else if (e.target.closest('.send-back')) {
    selectedObject.sendToBack();
    canvas.renderAll();
  }
});
